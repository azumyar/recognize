import numpy as np
from typing import Any, NamedTuple, Callable

import src.exception as ex
import src.recognition as recognition


class TranslateResult(NamedTuple):
    """
    RecognitionModel#transcribeの戻り値データ型
    """
    translate:str
    extend_data:Any


class TranslateModel:
    """
    認識モデル抽象基底クラス
    """

    @property
    def required_sample_rate(self) -> int | None:
        ...

    def translate(self, audio_data:np.ndarray) -> TranslateResult:
        ...

try:
    from transformers import pipeline  # type: ignore
    import stable_whisper # type: ignore
    import torch # type: ignore
    from typing import Optional
except:
    pass
else:
    class TranslateModelKotobaWhisperBIL(TranslateModel, recognition.RecognitionModel):
        def __init__(self, device:str, device_index:int) -> None:
            torch_dtype = torch.bfloat16 if device == "cuda" else torch.float32
            model_kwargs:Any = {"attn_implementation": "sdpa"} if torch.cuda.is_available() else {}
            model_kwargs["torch_dtype"] = torch_dtype

            if device == "cuda":
                device = f"{device}:{device_index}"
            self.__pipe = pipeline(
                "automatic-speech-recognition",
                model="kotoba-tech/kotoba-whisper-bilingual-v1.0",
                device=device,
                model_kwargs=model_kwargs,
                chunk_length_s=15,
                batch_size=16
            )
            self.__generate_kwargs_translate = {"language": "en", "task": "translate"}
            self.__generate_kwargs_ttranscribe = {"language": "ja", "task": "transcribe"}

        @property
        def required_sample_rate(self) -> int | None:
            return 16000

        def translate(self, audio_data:np.ndarray) -> TranslateResult:
            reslut = self.__pipe(
                audio_data.astype(np.float16) / float(np.iinfo(np.int16).max),
                generate_kwargs = self.__generate_kwargs_translate)
            r:str = reslut["text"] #type: ignore
            return TranslateResult(r, reslut)

        def get_verbose(self, _:int) -> str | None:
            return None

        def transcribe(self, audio_data:np.ndarray) -> recognition.TranscribeResult:
            reslut = self.__pipe(
                audio_data.astype(np.float16) / float(np.iinfo(np.int16).max),
                generate_kwargs = self.__generate_kwargs_ttranscribe)
            if "chunks" in reslut and len(reslut["chunks"]) == 1 and "timestamp" in reslut["chunks"][0]: #type: ignore
                ts = reslut["chunks"][0]["timestamp"] #type: ignore
                if ts[0] == 0.0 and ts[1] == 0.1:
                    raise recognition.TranscribeException(f"ノイズ判定:{reslut}") 
            r = reslut["text"] #type: ignore
            if isinstance(r, str):
                return recognition.TranscribeResult(r, reslut)
            if isinstance(r, list):
                return recognition.TranscribeResult("".join(r), reslut)
            raise ex.ProgramError(f"pipelineから意図しない戻り値型:{type(r)}")

#class TranslateException(ex.IlluminateException):
#    """
#    認識に失敗した際なげる例外
#    """
#    pass
