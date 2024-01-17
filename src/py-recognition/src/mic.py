import queue
import time
from typing import Optional, Callable
import speech_recognition as sr

import src.exception as ex
from src.cancellation import CancellationObject

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
        mic_index:Optional[int]) -> None:

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

        if not mic_index is None:
            audio = sr.Microphone.get_pyaudio().PyAudio()
            try:
                    check_mic(audio, mic_index, sample_rate)
            finally:
                audio.terminate()

        self.__sample_rate = sample_rate
        self.__audio_queue = queue.Queue()
        self.__source = sr.Microphone(
            sample_rate = sample_rate,
            device_index = mic_index)        
        self.__recorder = sr.Recognizer()
        self.__recorder.energy_threshold = energy
        self.__recorder.pause_threshold = pause
        if self.__recorder.pause_threshold < self.__recorder.non_speaking_duration:
            self.__recorder.non_speaking_duration = pause / 2
        self.__recorder.dynamic_energy_threshold = dynamic_energy

        self.__device_name = "デフォルトマイク" if mic_index is None else self.__source.list_microphone_names()[mic_index]

        with self.__source as mic:
            self.__recorder.adjust_for_ambient_noise(mic)

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

    def listen(self, onrecord:Callable[[bytes], None], timeout=None, phrase_time_limit=None) -> None:
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
            onrecord(audio_data)
        except sr.WaitTimeoutError:
            pass
        except sr.UnknownValueError:
            pass

    def listen_loop(self, onrecord:Callable[[bytes], None], cancel:CancellationObject, phrase_time_limit=None) -> None:
        """
        マイクループ。処理を返しません。
        """
        def record(_, audio:sr.AudioData) -> None:
            self.__audio_queue.put_nowait(audio.get_raw_data())

        stop = self.__recorder.listen_in_background(self.__source, record, phrase_time_limit=phrase_time_limit)
        try:
            while cancel.alive:
                if not self.__audio_queue.empty():
                    audio_data = self.__get_audio_data()
                    onrecord(audio_data)
                time.sleep(0.1)
        finally:
            stop(False)

    def test_mic(self, cancel:CancellationObject):
        import speech_recognition.exceptions

        timout = 5
        phrase_time_limit = 0.5
        print("マイクテストを行います")
        print(f"{int(timout)}秒間マイクを監視し音を拾った場合その旨を表示します")
        print(f"使用マイク:{self.device_name}")
        print(f"energy_threshold:{self.__recorder.energy_threshold}")
        print(f"pause_threshold:{self.__recorder.pause_threshold}")
        print(f"non_speaking_duration:{self.__recorder.non_speaking_duration}")
        print(f"dynamic_energy_threshold:{self.__recorder.dynamic_energy_threshold}")
        print("終了する場合はctr+cを押してください")
        print("")
        try:
            while cancel.alive:
                print("計測開始")
                try:
                    with self.__source as microphone:
                        audio = self.__recorder.listen(
                            source = microphone,
                            timeout = timout,
                            phrase_time_limit = phrase_time_limit)
                    if len(audio.get_raw_data()) == 0:
                        pass
                    else:
                        print("音を拾いました")
                except speech_recognition.exceptions.WaitTimeoutError:
                    pass
                print("")
                time.sleep(1)
        finally:
            pass

class MicInitializeExeception(ex.IlluminateException):
    pass