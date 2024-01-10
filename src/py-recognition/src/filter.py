import numpy as np

class NoiseFilter:
    def __init__(self, sampling_rate:int) -> None:
        self.sampling_rate = sampling_rate

    def filter(self, data:np.ndarray): # np.ndarray[np.complex128]
        ...

class LowPassFilter(NoiseFilter):
    def __init__(   
        self,
        sampling_rate:int,
        cutoff:int=200,
        cutoff_upper:int=200) -> None:
        super().__init__(sampling_rate)
        self.cutoff = cutoff
        self.cutoff_upper = sampling_rate - cutoff_upper

    def filter(self, data:np.ndarray):
        freq = np.fft.fftfreq(data.size, 1.0 / self.sampling_rate)
        cutoff = self.cutoff * np.pi / 2
        cutoff_upper = self.cutoff_upper * np.pi / 2 * -1
        data[(freq > cutoff) | (freq < cutoff_upper)] = 0.0

class HighPassFilter(NoiseFilter):
    def __init__(   
        self,
        sampling_rate:int,
        cutoff:int=200,
        cutoff_upper:int=200) -> None:
        super().__init__(sampling_rate)
        self.cutoff = cutoff
        self.cutoff_upper = sampling_rate - cutoff_upper

    def filter(self, data:np.ndarray):
        freq = np.fft.fftfreq(data.size, 1.0 / self.sampling_rate)
        cutoff = self.cutoff * np.pi / 2
        cutoff_upper = self.cutoff_upper * np.pi / 2 * -1
        data[((freq > 0) & (freq < cutoff)) | ((freq < 0) & (freq > -cutoff_upper))] = 0.0
