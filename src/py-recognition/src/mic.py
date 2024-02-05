import queue
import time
import multiprocessing
import speech_recognition as sr
import speech_recognition.exceptions
from concurrent.futures import ThreadPoolExecutor
from typing import Callable

import src.exception as ex
from src.cancellation import CancellationObject
from src.interop import print

class Mic:
    """
    マイク操作クラス
    """

    __HOSTAPI_WASAPI = 2
    __HOSTAPI_KS = 3

    def __init__(
        self,
        sample_rate:int,
        energy:float,
        pause:float,
        dynamic_energy:bool,
        phrase:float | None,
        non_speaking:float | None,
        mic_index:int | None) -> None:

        def check_mic(audio, mic_index:int, sample_rate:int):
            try:
                s = sr.Microphone.MicrophoneStream(
                    audio.open(
                        input = True,
                        input_device_index = mic_index,
                        channels = 1,
                        format = sr.Microphone.get_pyaudio().paInt16,
                        rate = sample_rate,
                        frames_per_buffer = 1024,
                    ))
                s.close()
            except Exception as e:
                raise MicInitializeExeception("マイクの初期化に失敗しました", e)
            finally:
                pass

        audio = sr.Microphone.get_pyaudio().PyAudio()
        try:
            if not mic_index is None:
                check_mic(audio, mic_index, sample_rate)
                self.__device_name = audio.get_device_info_by_index(mic_index).get("name")
            else:
                self.__device_name = "デフォルトマイク"
        finally:
            audio.terminate()

        self.__mic_index = mic_index
        self.__energy = energy
        self.__pause = pause
        self.__dynamic_energy = dynamic_energy
        self.__phrase = phrase
        self.__non_speaking = non_speaking

        self.__sample_rate = sample_rate
        self.__audio_queue = queue.Queue()
        self.__source = Mic.__create_mic(sample_rate, mic_index)        
        self.__recorder = Mic.__create_recognizer(energy, pause, dynamic_energy, phrase, non_speaking)

        with self.__source as mic:
            self.__recorder.adjust_for_ambient_noise(mic)

    @staticmethod
    def __create_mic(sample_rate:int, mic_index:int | None) -> sr.Microphone:
        return sr.Microphone(
            sample_rate = sample_rate,
            device_index = mic_index)

    @staticmethod
    def __create_recognizer(energy:float, pause:float, dynamic_energy:bool, phrase:float | None, non_speaking:float | None,) -> sr.Recognizer:
        r = sr.Recognizer()
        r.energy_threshold = energy
        r.pause_threshold = pause
        if non_speaking is None:
            if r.pause_threshold < r.non_speaking_duration:
                r.non_speaking_duration = pause / 2
        else:
            r.non_speaking_duration = non_speaking
        if not phrase is None:
            r.phrase_threshold = phrase
        r.dynamic_energy_threshold = dynamic_energy
        return r

    @staticmethod
    def update_sample_rate(mic_index:int | None, sample_rate:int) -> int:
        ret = sample_rate
        if not mic_index is None:
            audio = sr.Microphone.get_pyaudio().PyAudio()
            try:
                device_info = audio.get_device_info_by_index(mic_index)
                host = device_info.get("hostApi")
                # WASAPI共有モードはサンプリングレートをデバイスと一致させる必要がある
                if isinstance(host, int) and host == Mic.__HOSTAPI_WASAPI:
                    rate = device_info.get("defaultSampleRate")
                    assert isinstance(rate, float)
                    ret = int(rate)
            finally:
                audio.terminate()
        return ret

    @property
    def device_name(self):
        return self.__device_name
    
    @property
    def sample_rate(self):
        return self.__sample_rate

    def __get_audio_data(self, min_time:float=-1.) -> bytes:
        audio = bytes()
        is_goted = False
        start_time = time.time()
        while not is_goted or time.time() - start_time < min_time:
            time.sleep(0.01)
            while not self.__audio_queue.empty():
                audio += self.__audio_queue.get()
                is_goted = True
        return sr.AudioData(audio, self.__sample_rate, 2).get_raw_data()

    def listen(self, onrecord:Callable[[int, bytes], None], timeout=None, phrase_time_limit=None) -> None:
        """
        一度だけマイクを拾う
        """
        try:
            with self.__source as microphone:
                audio = self.__recorder.listen(
                    source = microphone,
                    timeout = timeout,
                    phrase_time_limit = phrase_time_limit)
            self.__audio_queue.put_nowait(audio.get_raw_data())
            audio_data = self.__get_audio_data()
            onrecord(1, audio_data)
        except sr.WaitTimeoutError:
            pass
        except sr.UnknownValueError:
            pass

    def listen_loop(self, onrecord:Callable[[int, bytes], None], cancel:CancellationObject, phrase_time_limit=None) -> None:
        """
        マイクループ。処理を返しません。
        """
        def listen_mic():
            with self.__source as s:
                while cancel.alive:
                    try:
                        audio = self.__recorder.listen(s, 0.25, phrase_time_limit)
                    except speech_recognition.exceptions.WaitTimeoutError:
                        pass
                    else:
                        if cancel.alive:
                            self.__audio_queue.put_nowait(audio.get_raw_data())

        thread_pool = ThreadPoolExecutor(max_workers=2)
        thread_pool.submit(listen_mic)
        try:
            index = 1
            while cancel.alive:
                if not self.__audio_queue.empty():
                    audio_data = self.__get_audio_data()
                    onrecord(index, audio_data)
                    index += 1
                time.sleep(0.1)
        finally:
            thread_pool.shutdown(wait=False)

    @staticmethod
    def listen_mic_process(
        sample_rate:int,
        energy:float,
        pause:float,
        dynamic_energy:bool,
        phrase:float | None,
        non_speaking:float | None,
        mic_index:int | None,
        cancel,
        out:multiprocessing.Queue):

        mic = Mic.__create_mic(sample_rate, mic_index)
        rec = Mic.__create_recognizer(energy, pause, dynamic_energy, phrase, non_speaking)
        with mic as s:
            rec.adjust_for_ambient_noise(s)
            while cancel.value != 0:
                try:
                    audio = rec.listen(s, 0.25, None)
                except speech_recognition.exceptions.WaitTimeoutError:
                    pass
                else:
                    if cancel.value != 0:
                        out.put_nowait(audio.get_raw_data())

    def listen_loop_mp(self, queue:multiprocessing.Queue, cancel) -> None:
        """
        マイクループ。処理を返しません。(マルチプロセス実装)
        """

        p = multiprocessing.Process(
            target=Mic.listen_mic_process, 
            args=(
                self.__sample_rate,
                self.__energy,
                self.__pause,
                self.__dynamic_energy,
                self.__phrase,
                self.__non_speaking,
                self.__mic_index,
                cancel,
                queue))
        p.daemon = True
        p.start()

        try:
            while cancel.value != 0:
                time.sleep(0.1)
        finally:
            cancel.value = 0 # type: ignore
            p.join()

    def test_mic(self, cancel:CancellationObject, onrecord:Callable[[int, bytes], None] | None = None):
        timemax = 5
        timeout = 0.25
        print("マイクテストを行います")
        print(f"{int(timemax)}秒間マイクを監視し音を拾った場合その旨を表示します")
        print(f"使用マイク:{self.device_name}")
        print(f"energy_threshold:{self.__recorder.energy_threshold}")
        print(f"pause_threshold:{self.__recorder.pause_threshold}")
        print(f"non_speaking_duration:{self.__recorder.non_speaking_duration}")
        print(f"dynamic_energy_threshold:{self.__recorder.dynamic_energy_threshold}")
        print("終了する場合はctr+cを押してください")
        print("")

        try:
            index = 1
            while cancel.alive:
                print(f"計測開始 #{str(index).zfill(2)}")
                audio:sr.AudioData | None = None
                for _ in range(int(timemax / timeout)):
                    try:
                        with self.__source as microphone:
                            audio = self.__recorder.listen(
                                source = microphone,
                                timeout = timeout,
                                phrase_time_limit = None)
                    except speech_recognition.exceptions.WaitTimeoutError:
                        pass
                    else:
                        break
                if not audio is None:
                    b = audio.get_raw_data()
                    sec = len(b) / 2 / self.__sample_rate
                    print("認識終了")
                    print(f"{round(sec, 2)}秒音を拾いました")
                    if not onrecord is None:
                        onrecord(index, b)
                index += 1
                print("")
                time.sleep(1)
        finally:
            pass

class MicInitializeExeception(ex.IlluminateException):
    pass