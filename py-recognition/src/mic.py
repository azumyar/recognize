import queue
import time
from typing import Optional, Callable
import speech_recognition as sr

from src.cancellation import CancellationObject

class Mic:
    """
    マイク操作クラス
    """
    def __init__(
        self,
        sample_rate:int,
        energy:float,
        pause:float,
        dynamic_energy:bool,
        mic_index:Optional[int]) -> None:

        self.__sample_rate = sample_rate
        self.__audio_queue = queue.Queue()

        self.__source = sr.Microphone(
            sample_rate = sample_rate,
            device_index = mic_index)
        self.__recorder = sr.Recognizer()
        self.__recorder.energy_threshold = energy
        self.__recorder.pause_threshold = pause
        self.__recorder.dynamic_energy_threshold = dynamic_energy
        with self.__source:
            self.__recorder.adjust_for_ambient_noise(self.__source)

        self.device_name_val = "デフォルトマイク" if mic_index is None else self.__source.list_microphone_names()[mic_index]

    @property
    def device_name(self):
        return self.device_name_val
    
    def __get_audio_data(self, min_time:float=-1.) -> bytes:
        audio = bytes()
        is_goted = False
        start_time = time.time()
        while not is_goted or time.time() - start_time < min_time:
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

