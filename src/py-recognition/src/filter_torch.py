import numpy
from src.filter import VoiceActivityDetectorFilter

try:
    import torch
except:
    pass
else:
    class SileroVadFilter(VoiceActivityDetectorFilter):
        def __init__(
                self,
                sample_rate:int,
                threshold:float = 0.5,
                min_speech_duration:float = 0.25,
                min_silence_duration:float = 0.1):
            torch.set_num_threads(1)
            model, utils = torch.hub.load(
                repo_or_dir="snakers4/silero-vad",
                model="silero_vad",
                trust_repo=True) # type: ignore

            (get_speech_timestamps, _, _, _, _) = utils
            self.__sample_rate = sample_rate
            self.__model = model
            self.__get_speech_timestamps = get_speech_timestamps
            self.__threshold = threshold
            self.__min_speech_duration_ms = int(min_speech_duration * 1000)
            self.__min_silence_duration_ms = int(min_silence_duration * 1000)

        def check(self, data:bytes) -> bool:
            wav = torch.from_numpy(numpy.frombuffer(data, dtype=numpy.int16).astype(numpy.float32)).clone()
            speech_timestamps = self.__get_speech_timestamps(
                wav,
                self.__model,
                sampling_rate = self.__sample_rate,
                threshold = self.__threshold,
                min_speech_duration_ms = self.__min_speech_duration_ms,
                min_silence_duration_ms = self.__min_silence_duration_ms)
            #def get_speech_timestamps(audio: torch.Tensor,
            #              model,
            #              threshold: float = 0.5,
            #              sampling_rate: int = 16000,
            #              min_speech_duration_ms: int = 250,
            #              max_speech_duration_s: float = float('inf'),
            #              min_silence_duration_ms: int = 100,
            #              speech_pad_ms: int = 30,
            #              return_seconds: bool = False,
            #              visualize_probs: bool = False,
            #              progress_tracking_callback: Callable[[float], None] = None,
            #              window_size_samples: int = 512,):

            return 0 < len(speech_timestamps)