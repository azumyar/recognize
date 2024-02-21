import speech_recognition
import os
import traceback
from typing import Any, Callable, Iterable, Optional, NamedTuple

from src import Logger, Enviroment


class Record(NamedTuple):
    """
    録音設定
    """
    is_record:bool
    file:str
    directory:str


def is_feature(feature:str, func:str) -> bool:
    """
    featureにfuncが含まれるか判定
    """
    def strip(s:str) -> str:
        return str.strip(s)
    return func in map(strip, feature.split(","))


def save_wav(record:Record, index:int, data:bytes, sampling_rate:int, sample_width:int, logger:Logger) -> None:
    """
    音声データをwavに保存
    """
    if record.is_record:
        try:
            with open(f"{record.directory}{os.sep}{record.file}-{str(index).zfill(4)}.wav", "wb") as fout:      
                fout.write(speech_recognition.AudioData(data, sampling_rate, 2).get_wav_data())
        except OSError as e:
            logger.error([
                "##########################",
                "wavファイルの保存に失敗しました",
                str(e),
                traceback.format_exc(),
                "##########################"
            ])