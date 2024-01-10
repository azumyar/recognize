import torch
import whisper
import faster_whisper as fwis
import numpy as np
import speech_recognition as sr
import urllib.error as urlerr
from typing import Any, Optional

import src.exception as ex
import src.google_recognizers as google


class AudioModel:
    def transcribe(self, _:np.ndarray) -> tuple[str, Any]:
        ...

class AudioModelWhisper(AudioModel):
    def __init__(
        self,
        model:str,
        language:str,
        device:str,
        download_root:str) -> None:
        self.device = device
        self.language = language if language != "" else None

        m = f"{model}.{language}" if (model != "large") and (model != "large-v2") and (model != "large-v3") and (language == "en") else model
        self.audio_model = whisper.load_model(m, download_root=download_root).to(device)

    def transcribe(self, na:np.ndarray) -> tuple[str, Any]:
        r = self.audio_model.transcribe(
            torch.from_numpy(na.astype(np.float32) / float(np.iinfo(np.int16).max)),
            language = self.language,
            fp16 = self.device == "cuda")["text"]
        if isinstance(r, str):
            return (r, None)
        if isinstance(r, list):
            return ("".join(r), None)
        raise RuntimeError(f"Whisper.transcribeから意図しない戻り値型:{type(r)}")

class AudioModelWhisperFaster(AudioModel):
    def __init__(
        self,
        model:str,
        language:str,
        device:str,
        download_root:str) -> None:
        self.device = device
        self.language = language if language != "" else None

        m = f"{model}.{language}" if (model != "large") and (model != "large-v2") and (language == "en") else model
        self.audio_model = fwis.WhisperModel(
            m,
            device,
            compute_type = "float16" if device == "cuda" else "int8",
            download_root=download_root)

    def transcribe(self, na:np.ndarray)  -> tuple[str, Any]:
        segments, _  = self.audio_model.transcribe(
            na.astype(np.float32) / float(np.iinfo(np.int16).max),
            language = self.language)
        c = []
        for s in segments:
            c.append(s.text)
        return ("".join(c), segments)

class AudioModelGoogle(AudioModel):
    def __init__(
        self,
        sample_rate:int,
        sample_width:int,
        language:str="ja-JP",
        key:str | None=None,
        timeout:float | None = None,
        challenge:int = 1):
        self.sample_rate = sample_rate
        self.sample_width = sample_width
        self.language = language
        self.key = key
        self.operation_timeout = timeout
        self.max_loop = challenge

    def transcribe(self, na:np.ndarray) -> tuple[str, Any]:
        data = sr.AudioData(na.astype(np.int16, order="C"), self.sample_rate, self.sample_width)
        loop = 0

        while loop < self.max_loop:
            try:
                r = google.recognize_google(
                    google.encode_falc(data),
                    self.operation_timeout,
                    language=self.language,
                    key = self.key)
                return (r[0], r[2])

            except urlerr.HTTPError as e:
                raise TranscribeException("google音声認識でHTTPエラー: {}".format(e.reason), e)
            except urlerr.URLError as e:
                print(vars(e))
                raise TranscribeException("google音声認識でリモート接続エラー: {}".format(e.reason), e)
            except sr.UnknownValueError as e:
                raise TranscribeException(
                    "googleは音声データを検出できませんでした",
                    e)
            except google.UnknownValueErrorMod as e:
                raise TranscribeException(
                    f"googleは音声データを検出できませんでした_mod",
                    e)
            except TimeoutError:
                if self.max_loop == 1:
                    raise TranscribeException(f"google音声認識でリモート接続がタイムアウトしました")
        loop += 1
        raise TranscribeException(f"{self.max_loop}回試行しましたが失敗しました")
 
class TranscribeException(ex.IlluminateException):
    pass
