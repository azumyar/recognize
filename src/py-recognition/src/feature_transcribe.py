import audioop
import wave
import glob
import numpy

import src.microphone
import src.recognition
import src.val as val
from src import Enviroment, Logger
from src.cancellation import CancellationObject

def run(
    path:str,
    recognition_model:src.recognition.RecognitionModel,
    env:Enviroment,
    logger:Logger,
    feature:str) -> None:

    for file in glob.glob(path):
        logger.print(f"transcribe: {file}")
        try:
            wf = wave.open(file, "rb")
            logger.print(f"width: {wf.getsampwidth()}")
            logger.print(f"channels: {wf.getnchannels()}")
            logger.print(f"rate: {wf.getframerate()}")

            d = wf.readframes(-1)
            #if recognition_model.required_sample_rate is not None:
            d, _ = audioop.ratecv(
                d,
                2, # sample_width
                1,
                wf.getframerate(),
                16000,
                None)
            ret = recognition_model.transcribe(numpy.frombuffer(d, dtype=numpy.int16).flatten())
            logger.print(ret.transcribe, console=val.Console.Cyan, reset_console=True)
        except wave.Error as e:
            logger.print("!!!wave.Error!!!", console=val.Console.Yellow, reset_console=True)
            logger.print(e, console=val.Console.Yellow, reset_console=True)
        except src.recognition.TranscribeException as e:
            logger.print("!!!TranscribeException!!!", console=val.Console.Yellow, reset_console=True)
            logger.print(e, console=val.Console.Yellow, reset_console=True)
        except Exception as e:
            logger.print("!!!Unhandled Exception!!!", console=val.Console.Red, reset_console=True)
            logger.print(e, console=val.Console.Red, reset_console=True)
        logger.print(" ") #改行
    logger.print(f"done.")
