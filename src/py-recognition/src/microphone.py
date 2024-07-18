import audioop
import collections
import queue
import sounddevice
import numpy as np
from typing import Any, Callable, Deque, NamedTuple

import src.mic
import src.filter as filter
from src.cancellation import CancellationObject

class Microphone:
    def __init__(
            self,
            energy_threshold,
            filter_vad:filter.VoiceActivityDetectorFilter,
            device:int|None) -> None:
        self.__energy_threshold = energy_threshold
        self.__filter_vad = filter_vad
        self.filter_highPass  = None
        self.__device = device

        self.sample_rate = 16000

    @staticmethod
    def query_devices() -> list:
        r = []
        for hostapi in sounddevice.query_hostapis():
            if hostapi["name"].lower() == "mme": #type: ignore
                for device_numbar in hostapi["devices"]: #type: ignore
                    device = sounddevice.query_devices(device=device_numbar)
                    if 0 < device["max_input_channels"]: #type: ignore
                        r.append(f"{device_numbar} {device['name']}") #type: ignore
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
                        frames.append(buffer)
                        break
                print("Phase.2")
                while True:
                    buffer = q.get()
                    buffer = self.filter(buffer)
                    energy = audioop.rms(buffer, 2)
                    frames.append(buffer)
                    if not self.__filter_vad.check(buffer):
                       break
                print("done.")
                frame_data = b"".join(map(lambda x: x, frames))
                index += 1
                onrecord(index, src.mic.ListenResultParam(frame_data, src.mic.ListenEnergy(0, 0, 0)))



    def filter(self, buffer:bytes) -> bytes:
        if self.filter_highPass is None:
            return buffer
        else:
            fft = np.fft.fft(np.frombuffer(buffer, np.int16).flatten())
            self.filter_highPass.filter(fft)
            return np.real(np.fft.ifft(fft)).astype(np.uint16, order="C").tobytes()