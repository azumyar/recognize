import audioop
import collections
import queue
import sounddevice
import math
import numpy as np
from typing import Any, Callable, Deque, NamedTuple

from src import Logger, rms2db
import src.recognition as recognition
import src.filter as filter
import src.val as val
from src.cancellation import CancellationObject

class ListenEnergy(NamedTuple):
    value:float
    max:float
    min:float


class ListenResultParam(NamedTuple):
    '''listenのコールバックパラメータ'''
    pcm:bytes
    '''PCMバイナリデータ'''
    energy:ListenEnergy | None
    '''pcmのRMS値(src.mic.AudioDataのみに格納)'''

class Device(NamedTuple):
    index:int
    hostapi:str
    name:str

    def __str__(self) -> str:
        return f"{self.index} : {self.name}"

class Microphone:
    __BAR_COLOR_NONE = val.Console.background_index(240)
    __BAR_COLOR_ENAGY_OK = val.Console.background_index(35)
    __BAR_COLOR_VAD_OK = val.Console.background_index(39)
    __BAR_COLOR_BACKGROUND = val.Console.background_index(234)

    def __init__(
            self,
            energy_threshold:float,
            mp_recog_conf:recognition.RecognizeMicrophoneConfig,
            filter_vad:filter.VoiceActivityDetectorFilter,
            filter_highPass:filter.HighPassFilter | None,
            phase2_sec:float,
            device:int|None,
            logger:Logger) -> None:
        self.__energy_threshold = energy_threshold
        self.__recog_conf = mp_recog_conf
        self.__filter_vad = filter_vad
        self.__filter_highPass = filter_highPass
        self.__device = device
        self.__vad_sec = 0.5
        self.__vad_phase2_sec = phase2_sec
        self.__sample_rate = val.MIC_SAMPLE_RATE
        self.__sample_width = val.MIC_SAMPLE_WIDTH
        self.__chunk_size = 1024
        self.__logger = logger

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
    def start_insert_sec(self) -> float: return self.__recog_conf.head_insert_duration

    @property
    def end_insert_sec(self) -> float: return self.__recog_conf.tail_insert_duration

    @property
    def sample_rate(self) -> int: return self.__sample_rate
    @property
    def sample_width(self) -> int: return self.__sample_width
    @property
    def chunk_size(self) -> int: return self.__chunk_size

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

    def listen(
        self,
        onrecord:Callable[[int, ListenResultParam], None],
        cancel:CancellationObject,
        opt_enable_energy_threshold:bool = True,
        opt_enable_indicator:bool|None = None):
        index = 0

        if opt_enable_energy_threshold:
            energy_threshold = self.energy_threshold
        else:
            energy_threshold = 0
        if not opt_enable_indicator is None and opt_enable_indicator:
            _print = print
        else:
            _print = self.__logger.notice

        with sounddevice.RawInputStream(
            samplerate=self.__sample_rate,
            blocksize=self.__chunk_size,
            channels=1,
            dtype="int16",
            device=self.__device) as stream:

            dB = 0.0
            vad_list_len = math.ceil(self.sample_rate * self.__vad_sec / self.__chunk_size)
            vad_phase2_len = max(1, math.ceil(self.sample_rate * self.__vad_phase2_sec / self.__chunk_size))
            while cancel.alive:
                frames = collections.deque()
                is_overflowed = False

                # chunk_sizeが小さい場合VADが認識しないのでvad_secバッファをためてVADにかける
                #print("Phase.0")
                for _ in range(vad_list_len):
                    self.__indicate(dB, _print)

                    _buffer, overflowed = stream.read(self.__chunk_size)
                    if(overflowed):
                        self.__logger.error("overflowed")
                        is_overflowed = True
                        break
                    buffer = self.filter(bytes(_buffer)) # type: ignore
                    en = audioop.rms(buffer, val.MIC_SAMPLE_WIDTH)
                    dB = rms2db(en)
                    frames.append((buffer, en))

                # 開始音検索位置
                #print("Phase.1")
                if is_overflowed:
                    continue
                while True:
                    self.__indicate(dB, _print)

                    b = b"".join(map(lambda x:x[0], frames))
                    energy = audioop.rms(b, val.MIC_SAMPLE_WIDTH)
                    if energy_threshold < energy and self.__filter_vad.check(b):
                        break
                    else:
                        frames.popleft()

                    _buffer, overflowed = stream.read(self.__chunk_size)
                    if(overflowed):
                        self.__logger.error("overflowed")
                        break
                    buffer = self.filter(bytes(_buffer)) # type: ignore
                    en = audioop.rms(buffer, val.MIC_SAMPLE_WIDTH)
                    dB = rms2db(en)
                    frames.append((buffer, en))

                # 声が含まれなくなるまでVADにかける
                #print("Phase.2")
                if is_overflowed:
                    continue
                while True:
                    temp = []
                    for _ in range(vad_phase2_len):
                        _buffer, overflowed = stream.read(self.__chunk_size)
                        if(overflowed):
                            self.__logger.error("overflowed")
                            break
                        buffer = self.filter(bytes(_buffer)) # type: ignore
                        temp.append(buffer)
                        db = rms2db(audioop.rms(buffer, val.MIC_SAMPLE_WIDTH))
                        self.__indicate_pahse2(db, _print)


                    buffer = b"".join(temp)
                    frames.append((buffer, audioop.rms(buffer, val.MIC_SAMPLE_WIDTH)))
                    b = b"".join(map(lambda x: x[0], frames))
                    if not self.__filter_vad.check(b[-(vad_list_len * self.__chunk_size * 2):]):
                       break
                if is_overflowed:
                    continue
                print("\033[1G\033[0K", end="", flush=True)

                # 先頭末尾無音追加処理
                head = b""
                tail = b""
                if 0 < self.__recog_conf.head_insert_duration:
                    mx = math.ceil(self.__sample_rate * self.__recog_conf.head_insert_duration)
                    head = b"".join(map(lambda _: b"00", range(mx)))
                if 0 < self.__recog_conf.tail_insert_duration:
                    mx = math.ceil(self.__sample_rate * self.__recog_conf.tail_insert_duration)
                    tail = b"".join(map(lambda _: b"00", range(mx)))
                frame_data = head + b"".join(map(lambda x: x[0], frames)) + tail
                index += 1
                onrecord(index, ListenResultParam(
                    frame_data,
                    ListenEnergy(
                        sum(map(lambda x: x[1], frames)) / len(frames),
                        max(map(lambda x: x[1], frames)),
                        min(map(lambda x: x[1], frames)))))

    def listen_ambient(self, record_sec:float) -> ListenEnergy:
        with sounddevice.RawInputStream(
            samplerate=self.__sample_rate,
            blocksize=self.__chunk_size,
            channels=1,
            dtype="int16",
            device=self.__device) as stream:

            c = []
            loop = math.ceil(self.__sample_rate * record_sec / self.__chunk_size)
            for _ in range(loop):
                _buffer, overflowed = stream.read(self.__chunk_size)
                buffer = bytes(_buffer) #type: ignore
                en = audioop.rms(buffer, val.MIC_SAMPLE_WIDTH)
                c.append(en)
                self.__indicate(rms2db(en), print)

            return ListenEnergy(
                sum(map(lambda x: x, c)) / len(c),
                max(map(lambda x: x, c)),
                min(map(lambda x: x, c)))

    def filter(self, buffer:bytes) -> bytes:
        if self.__filter_highPass is None:
            return buffer
        else:
            fft = np.fft.fft(np.frombuffer(buffer, np.int16).flatten())
            self.__filter_highPass.filter(fft)
            return np.real(np.fft.ifft(fft)).astype(np.uint16, order="C").tobytes()

    def __indicate(self, dB, print:Any):
        if rms2db(self.energy_threshold) < dB:
            color = Microphone.__BAR_COLOR_ENAGY_OK
        else:
            color = Microphone.__BAR_COLOR_NONE
        self.__print_dB("！", dB, color, print)

    def __indicate_pahse2(self, dB, print:Any):
        self.__print_dB("＃", dB, Microphone.__BAR_COLOR_VAD_OK, print)

    def __print_dB(self, str, dB, color:str, print:Any):
        MAX = 73

        if 80 < dB:
            db_text = "8+dB"
        else:
            db_text = f"{int(dB)}dB"

        dB = min(dB, 80)
        len = int(dB / 80 * MAX)
        a = "".join(map(lambda _: " ", range(len)))
        b = "".join(map(lambda _: " ", range(MAX - len)))
        #print("\033[1G", end="", flush=True)
        print(f"{str}{db_text}|{color}{a}{Microphone.__BAR_COLOR_BACKGROUND}{b}{val.Console.Reset.value}\r", end="")