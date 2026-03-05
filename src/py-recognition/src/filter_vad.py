
import io
import numpy 
import torchaudio
import scipy
import csv
from silero_vad import load_silero_vad, get_speech_timestamps

import src.interface as inf
from src.lazy_loader import tensorflow, tensorflow_hub


class VadFrame(object):
    """Represents a "frame" of audio data."""
    def __init__(self, bytes:bytes, timestamp, duration):
        self.bytes = bytes
        self.timestamp = timestamp
        self.duration = duration


class SileroVadFilter(inf.VoiceActivityDetectorFilter):
    """
    Silero-VADフィルタ
    """

    def __init__(   
        self,
        sampling_rate:int,
        threshold):

        self.__model = load_silero_vad()
        self.__sampling_rate = sampling_rate
        self.__threshold = threshold

    @property
    def mic_pause_duration(self) -> float:
        return 1.0

    def check(self, data:bytes) -> bool:
        bytes_io = io.BytesIO()
        raw_data = numpy.frombuffer(
            buffer=data, dtype=numpy.int16
        )
        scipy.io.wavfile.write(bytes_io, 16000, raw_data)

        auido, _ = torchaudio.load(bytes_io)
        speech_timestamps = get_speech_timestamps(
            auido,
            self.__model,
            threshold=self.__threshold,
            sampling_rate=self.__sampling_rate)
        return 0 < len(speech_timestamps)

class YAMNetVadFilter(inf.VoiceActivityDetectorFilter):
    """
    YAMNet-VADフィルタ
    """

    def __init__(   
        self,
        sampling_rate:int):

        self.__model = tensorflow_hub.load("https://tfhub.dev/google/yamnet/1")
        self.__classes = [
            "Speech",
            "Speech synthesizer",
            "Narration, monologue"
        ]

        # YAMNetクラス名一覧取得
        with tensorflow.io.gfile.GFile(self.__model.class_map_path().numpy()) as csvfile:
            reader = csv.DictReader(csvfile)
            self.__class_names = list(map(lambda x: x["display_name"], reader))

    @property
    def mic_pause_duration(self) -> float:
        return 0.4

    def check(self, data:bytes) -> bool:
        wav = numpy.frombuffer(data, dtype=numpy.int16)
        waveform = wav / tensorflow.int16.max

        scores, _, _ = self.__model(waveform)
        scores_np = scores.numpy()

        class_scores = {cls: sc for cls, sc in zip(self.__class_names, scores_np.mean(axis=0))}

        return 0.1 < sum(map(lambda x: class_scores[x], self.__classes))