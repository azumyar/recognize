import numpy as np
import webrtcvad

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
        self.__cutoff_upper = cutoff_upper

    def filter(self, data:np.ndarray):
        freq = np.fft.fftfreq(data.size, 1.0 / self.sampling_rate)
        cutoff = self.__cutoff
        cutoff_upper = self.__cutoff_upper
        #cutoff_upper = (1 / self.sampling_rate) - cutoff
        data[((freq > 0) & (freq < cutoff)) | ((freq < 0) & (freq > -cutoff_upper))] = 0.0

class VadFrame(object):
    """Represents a "frame" of audio data."""
    def __init__(self, bytes:bytes, timestamp, duration):
        self.bytes = bytes
        self.timestamp = timestamp
        self.duration = duration

class VoiceActivityDetectorFilter:
    """
    VADフィルタ
    """
    def __init__(   
        self,
        sampling_rate:int,
        vad_mode:int):
        self.__vad = webrtcvad.Vad(vad_mode)
        self.__sampling_rate = sampling_rate

    def check(self, data:bytes) -> bool:
        frame_duration_ms = 30
        return VoiceActivityDetectorFilter._check(
            self.__sampling_rate,
            frame_duration_ms, frame_duration_ms * 10,
            self.__vad,
            list(VoiceActivityDetectorFilter._frame_generator(frame_duration_ms, data, self.__sampling_rate)))


    @staticmethod
    def _frame_generator(frame_duration_ms:int, audio:bytes, sample_rate:int):
        """Generates audio frames from PCM audio data.

        Takes the desired frame duration in milliseconds, the PCM data, and
        the sample rate.

        Yields Frames of the requested duration.
        """
        n = int(sample_rate * (frame_duration_ms / 1000.0) * 2)
        offset = 0
        timestamp = 0.0
        duration = (float(n) / sample_rate) / 2.0
        while offset + n < len(audio):
            yield VadFrame(audio[offset:offset + n], timestamp, duration)
            timestamp += duration
            offset += n

    # 参考にしたオリジナルの実装
    @staticmethod
    def _vad_collector(
        sample_rate:int,
        frame_duration_ms:int,
        padding_duration_ms:int,
        vad:webrtcvad.Vad,
        frames: list[VadFrame],
        voice_trigger_on_thres:float=0.9,
        voice_trigger_off_thres: float=0.1) -> list[dict]:
        """音声非音声セグメント処理

        Args:
            sample_rate (int): 単位時間あたりのサンプル数[Hz]
            frame_duration_ms (int): フレーム長
            padding_duration_ms (int): ガード長
            vad (webrtcvad.Vad): _description_
            frames (list[Frame]): フレーム分割された音声データ
            voice_trigger_on_thres (float, optional): 音声セグメント開始と判断する閾値. Defaults to 0.9.
            voice_trigger_off_thres (float, optional): 音声セグメント終了と判断する閾値. Defaults to 0.1.

        Returns:
            list[dict]: セグメント結果
        """
        # ガードするフレーム数
        num_padding_frames = int(padding_duration_ms / frame_duration_ms)

        # バッファ(リングバッファではなくする)
        # ring_buffer = collections.deque(maxlen=num_padding_frames)
        frame_buffer = []

        # いま音声かどうかのトリガのステータス
        triggered = False

        voiced_frames = []
        vu_segments = []

        for frame in frames:
            is_speech = vad.is_speech(frame.bytes, sample_rate)
            frame_buffer.append((frame, is_speech))

            # 非音声セグメントの場合
            if not triggered:

                # 過去フレームのうち音声判定数を取得
                # 過去を見る数はnum_padding_frames個
                num_voiced = len([f for f, speech in frame_buffer[-num_padding_frames:] if speech])

                # 9割以上が音声の場合は音声にトリガする(立ち上がり)
                if num_voiced > voice_trigger_on_thres * num_padding_frames:
                    triggered = True

                    # num_padding_framesより前は非音声セグメントとする
                    audio_data = b''.join([f.bytes for f, _ in frame_buffer[:-num_padding_frames]])
                    vu_segments.append({"vad": 0, "audio_size": len(audio_data), "audio_data": audio_data})

                    # num_padding_frames以降は音声セグメント終了時にまとめるため一旦保持
                    for f, _ in frame_buffer[-num_padding_frames:]:
                        voiced_frames.append(f)
                    frame_buffer = []

            # 音声セグメントの場合
            else:
                # フレームを保持
                voiced_frames.append(frame)

                # 過去フレームのうち非音声判定数を取得
                # 過去を見る数はnum_padding_frames個
                num_unvoiced = len([f for f, speech in frame_buffer[-num_padding_frames:] if not speech])

                # 9割以上が非音声の場合はトリガを落とす(立ち下がり)
                if num_unvoiced > (1 - voice_trigger_off_thres) * num_padding_frames:
                    triggered = False

                    # 音声セグメントをまとめる
                    audio_data = b''.join([f.bytes for f in voiced_frames])
                    vu_segments.append({"vad": 1, "audio_size": len(audio_data), "audio_data": audio_data})
                    voiced_frames = []

                    frame_buffer = []

        # 終了時に音声セグメントか非音声セグメントかどうかで処理を分ける
        if triggered:
            audio_data = b''.join([f.bytes for f in voiced_frames])
            vu_segments.append({"vad": 1, "audio_size": len(audio_data), "audio_data": audio_data})
        else:
            audio_data = b''.join([f.bytes for f, _ in frame_buffer])
            vu_segments.append({"vad": 0, "audio_size": len(audio_data), "audio_data": audio_data})
        return vu_segments

    @staticmethod
    def _check(
        sample_rate:int,
        frame_duration_ms:int,
        padding_duration_ms:int,
        vad:webrtcvad.Vad,
        frames: list[VadFrame],
        voice_trigger_on_thres:float=0.9,
        voice_trigger_off_thres: float=0.1) -> bool:
        # ガードするフレーム数
        num_padding_frames = int(padding_duration_ms / frame_duration_ms)

        # バッファ(リングバッファではなくする)
        frame_buffer = []
        for frame in frames:
            is_speech = vad.is_speech(frame.bytes, sample_rate)
            frame_buffer.append((frame, is_speech))

            # 過去フレームのうち音声判定数を取得
            # 過去を見る数はnum_padding_frames個
            num_voiced = len([f for f, speech in frame_buffer[-num_padding_frames:] if speech])

            # 9割以上が音声の場合は音声にトリガする(立ち上がり)
            if num_voiced > voice_trigger_on_thres * num_padding_frames:
                return True
        return False
    
