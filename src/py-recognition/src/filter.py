import numpy as np

class NoiseFilter:
    """
    ノイズフィルタ抽象基底クラス
    """
    def __init__(self, sampling_rate:int) -> None:
        self.sampling_rate = sampling_rate

    def filter(self, data:np.ndarray): # np.ndarray[np.complex128]
        """
        ノイズフィルターをdataに対して行います。dataの内容は変更されます。
        """
        ...

class LowPassFilter(NoiseFilter):
    """
    ノイズフィルタのローパスフィルタ実装
    """
    def __init__(   
        self,
        sampling_rate:int,
        cutoff:int=0,
        cutoff_upper:int=200) -> None:
        super().__init__(sampling_rate)
        self.__cutoff = cutoff
        self.__cutoff_upper = sampling_rate - cutoff_upper

    def filter(self, data:np.ndarray):
        pass
        #freq = np.fft.fftfreq(data.size, 1.0 / self.sampling_rate)
        #cutoff = self.__cutoff
        #cutoff_upper = (1 / self.sampling_rate) - cutoff
        #data[((freq > cutoff)&(freq < cutoff_upper))] = 0 + 0j

class HighPassFilter(NoiseFilter):
    """
    ノイズフィルタのハイパスフィルタ実装
    """
    def __init__(   
        self,
        sampling_rate:int,
        cutoff:int=0,
        cutoff_upper:int=200) -> None:
        super().__init__(sampling_rate)
        self.__cutoff = cutoff
        self.__cutoff_upper = sampling_rate - cutoff_upper

    def filter(self, data:np.ndarray):
        freq = np.fft.fftfreq(data.size, 1.0 / self.sampling_rate)
        cutoff = self.__cutoff
        cutoff_upper = (1 / self.sampling_rate) - cutoff
        data[((freq > 0) & (freq < cutoff)) | ((freq < 0) & (freq > -cutoff_upper))] = 0.0
