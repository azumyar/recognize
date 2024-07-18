import audioop
import collections
import queue
import sounddevice
import math
import numpy as np
from typing import Any, Callable, Deque, NamedTuple

import src.mic
import src.recognition as recognition
import src.filter as filter
import src.val as val
from src.cancellation import CancellationObject


class Device(NamedTuple):
    index:int
    hostapi:str
    name:str

    def __str__(self) -> str:
        return f"{self.index} : {self.name}"


class Microphone:
    def __init__(
            self,
            energy_threshold:float,
            mp_recog_conf:recognition.RecognizeMicrophoneConfig,
            filter_vad:filter.VoiceActivityDetectorFilter,
            filter_highPass:filter.HighPassFilter | None,
            device:int|None) -> None:
        self.__energy_threshold = energy_threshold
        self.__recog_conf = mp_recog_conf
        self.__filter_vad = filter_vad
        self.filter_highPass = filter_highPass
        self.__device = device

        self.sample_rate = val.MIC_SAMPLE_RATE

        d = sounddevice.query_devices(
            device=device) if not device is None else sounddevice.query_devices(
                device=sounddevice.default.device[0],
                kind="input")
        if d is None:
            self.__device_name = "-不明なデバイス-"
        else:
            self.__device_name = d["name"] #type: ignore

    @property
    def device_name(self) -> str: return self.__device_name

    @property
    def energy_threshold(self) -> float: return self.__energy_threshold

    @property
    def end_insert_sec(self) -> float: return self.__recog_conf.delay_duration

    @staticmethod
    def query_devices() -> list[Device]:
        r:list[Device] = []
        for hostapi in sounddevice.query_hostapis():
            if hostapi["name"].lower() == "mme": #type: ignore
                for device_numbar in hostapi["devices"]: #type: ignore
                    device = sounddevice.query_devices(device=device_numbar)
                    if 0 < device["max_input_channels"]: #type: ignore
                        r.append(Device(device_numbar, hostapi["name"], device["name"])) #type: ignore
        return r

    def listen(self, onrecord:Callable[[int, src.mic.ListenResultParam], None], cancel:CancellationObject):
        q = queue.Queue()
        index = 0

        def callback(indata, frames, time, status):
           q.put(bytes(indata))
           pass
        
        with sounddevice.RawInputStream(
            samplerate=self.sample_rate,
            blocksize=int(self.sample_rate / 2),
            channels=1,
            callback=callback,
            dtype="int16",
            device=self.__device):
            while cancel.alive:
                frames = collections.deque()
                print("Phase.1")
                while True:
                    buffer = q.get()
                    buffer = self.filter(buffer)
                    energy = audioop.rms(buffer, 2)
                    if self.__energy_threshold < energy and self.__filter_vad.check(buffer):
                        frames.append((buffer, energy))
                        break
                print("Phase.2")
                while True:
                    buffer = q.get()
                    buffer = self.filter(buffer)
                    energy = audioop.rms(buffer, 2)
                    frames.append((buffer, energy))
                    if not self.__filter_vad.check(buffer):
                       break
                print("done.")

                if 0 < self.__recog_conf.delay_duration:
                    mx = math.ceil(self.sample_rate * self.__recog_conf.delay_duration)
                    frames.append((b"".join(map(lambda _: b"0", range(mx))), 0))
                frame_data = b"".join(map(lambda x: x[0], frames))
                index += 1
                onrecord(index, src.mic.ListenResultParam(frame_data, src.mic.ListenEnergy(0, 0, 0)))



    def filter(self, buffer:bytes) -> bytes:
        if self.filter_highPass is None:
            return buffer
        else:
            fft = np.fft.fft(np.frombuffer(buffer, np.int16).flatten())
            self.filter_highPass.filter(fft)
            return np.real(np.fft.ifft(fft)).astype(np.uint16, order="C").tobytes()