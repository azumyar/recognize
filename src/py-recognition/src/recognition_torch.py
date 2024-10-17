import os
import time
import math
import numpy as np
import speech_recognition as sr
import urllib.error as urlerr
from typing import Any, NamedTuple, Callable

import src.exception as ex
from src.recognition import RecognitionModel, TranscribeResult, TranscribeException


try:
    import whisper # type: ignore
    import torch # type: ignore
except:
    pass
else:
    class RecognitionModelWhisper(RecognitionModel):
        """
        認識モデルのwhisper実装
        """
        def __init__(
            self,
            model:str,
            language:str,
            device:str,
            download_root:str) -> None:
            self.__is_fp16 = device == "cuda"
            self.__language = language if language != "" else None

            m = f"{model}.{language}" if (model != "large") and (model != "large-v2") and (model != "large-v3") and (language == "en") else model
            self.audio_model = whisper.load_model(m, download_root=download_root).to(device)

        @property
        def required_sample_rate(self) -> int | None:
            return 16000

        def get_verbose(self, verbose:int) -> str | None:
            return None

        def transcribe(self, audio_data:np.ndarray) -> TranscribeResult:
            r = self.audio_model.transcribe(
                torch.from_numpy(audio_data.astype(np.float32) / float(np.iinfo(np.int16).max)),
                language = self.__language,
                fp16 = self.__is_fp16)["text"]
            if isinstance(r, str):
                return TranscribeResult(r, None)
            if isinstance(r, list):
                return TranscribeResult("".join(r), None)
            raise ex.ProgramError(f"Whisper.transcribeから意図しない戻り値型:{type(r)}")

        def get_log_info(self) -> str:
            return ""

try:
    import faster_whisper # type: ignore
    import torch # type: ignore
except:
    pass
else:
    class RecognitionModelWhisperFaster(RecognitionModel):
        """
        認識モデルのfaster_whisper実装
        """
        def __init__(
            self,
            model:str,
            language:str,
            device:str,
            device_index:int,
            download_root:str) -> None:
            self.__language = language if language != "" else None

            def get(device:str) -> tuple[str, str]:
                if device == "cuda":
                    try:
                        if torch.cuda.is_available():
                            mj, mi = torch.cuda.get_device_capability()
                            if 7 <= mj:
                                return ("cuda", "float16")
                            elif mj == 6 and 1 <= mi:
                                return ("cuda", "int8")
                            else:
                                return ("cpu", "int8")
                    except:
                        pass
                return ("cpu", "int8")

            m = f"{model}.{language}" if (model != "large") and (model != "large-v2") and (language == "en") else model
            run_device, compute_type = get(device)
            self.audio_model = faster_whisper.WhisperModel(
                m,
                run_device,
                device_index = device_index,
                compute_type = compute_type,
                download_root = download_root)

        @property
        def required_sample_rate(self) -> int | None:
            return 16000

        def get_verbose(self, verbose:int) -> str | None:
            return None

        def get_log_info(self) -> str:
            return ""

        def transcribe(self, audio_data:np.ndarray) -> TranscribeResult:
            segments, _  = self.audio_model.transcribe(
                audio_data.astype(np.float32) / float(np.iinfo(np.int16).max),
                language = self.__language,
                beam_size=5)
                #max_new_tokens = 128,
                #condition_on_previous_text = False)
            c = []
            for s in segments:
                c.append(s.text)
            return TranscribeResult("".join(c), segments)


try:
    from transformers import pipeline  # type: ignore
    import stable_whisper # type: ignore
    import torch # type: ignore
    from typing import Optional
except:
    pass
else:
    # kotoba-tech/kotoba-whisper-v1.1 がプログレス強制的に出すので入れ替える
    __adjust_by_silence = stable_whisper.WhisperResult.adjust_by_silence
    def __adjust_by_silence_mod(
            self,
            audio: torch.Tensor | np.ndarray | str | bytes,
            vad: bool = False,
            *k,
            verbose: bool | None = False,
            sample_rate: int | None = None,
            vad_onnx: bool = False,
            vad_threshold: float = 0.35,
            q_levels: int = 20,
            k_size: int = 5,
            min_word_dur: Optional[float] = None,
            word_level: bool = True,
            nonspeech_error: float = 0.3,
            use_word_position: bool = True) -> stable_whisper.WhisperResult:
            return __adjust_by_silence(
                self,
                audio= audio,
                vad= vad,
                *k,
                verbose= False,
                sample_rate= sample_rate, #type: ignore
                vad_onnx= vad_onnx,
                vad_threshold= vad_threshold,
                q_levels= q_levels,
                k_size= k_size,
                min_word_dur= min_word_dur,
                word_level= word_level,
                nonspeech_error= nonspeech_error,
                use_word_position= use_word_position)
    stable_whisper.WhisperResult.adjust_by_silence = __adjust_by_silence_mod

    class RecognitionModelWhisperKotoba(RecognitionModel):
        def __init__(self, device:str, device_index:int) -> None:
            model_id = "kotoba-tech/kotoba-whisper-v1.1"
            torch_dtype = torch.bfloat16 if device == "cuda" else torch.float32
            model_kwargs = {"attn_implementation": "sdpa"} if torch.cuda.is_available() else {}

            self.__generate_kwargs = {
                "language": "japanese",
                "task": "transcribe",      
            }
            if device == "cuda":
                device = f"{device}:{device_index}"
            self.__pipe = pipeline(
                model = model_id,
                torch_dtype = torch_dtype,
                device = device,
                model_kwargs = model_kwargs,
                trust_remote_code=True,
                chunk_length_s=15,
                batch_size=16,
                stable_ts=True,
                punctuator=False,
            )

        @property
        def required_sample_rate(self) -> int | None:
            return 16000

        def get_verbose(self, verbose:int) -> str | None:
            return None

        def transcribe(self, audio_data:np.ndarray) -> TranscribeResult:
            reslut = self.__pipe(
                audio_data.astype(np.float16) / float(np.iinfo(np.int16).max),
                return_timestamps=True,
                generate_kwargs = self.__generate_kwargs)
            if "chunks" in reslut and len(reslut["chunks"]) == 1 and "timestamp" in reslut["chunks"][0]: #type: ignore
                ts = reslut["chunks"][0]["timestamp"] #type: ignore
                if ts[0] == 0.0 and ts[1] == 0.1:
                    raise TranscribeException(f"ノイズ判定:{reslut}") 
            r = reslut["text"] #type: ignore
            if isinstance(r, str):
                return TranscribeResult(r, reslut)
            if isinstance(r, list):
                return TranscribeResult("".join(r), reslut)
            raise ex.ProgramError(f"pipelineから意図しない戻り値型:{type(r)}")

