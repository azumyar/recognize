from __future__ import annotations

import json
from urllib.parse import urlencode
from urllib.request import Request, urlopen

from speech_recognition.audio import AudioData
from speech_recognition.exceptions import RequestError, UnknownValueError

import src.exception as exception


server_url_recognize_ = "https://www.google.com/speech-api/v2/recognize"


def encode_falc(audio_data:AudioData) -> (bytes, str):
    flac_data = audio_data.get_flac_data(
        convert_rate=None if audio_data.sample_rate >= 8000 else 8000,  # audio samples must be at least 8 kHz
        convert_width=2  # audio samples must be 16-bit
    )
    return (flac_data, {"Content-Type": f"audio/x-flac; rate={audio_data.sample_rate}"})


def recognize_google_mod(recognizer, audio_data, key=None, language="en-US", pfilter=0, show_all=False, with_confidence=False):
    assert isinstance(audio_data, AudioData), "``audio_data`` must be audio data"
    assert key is None or isinstance(key, str), "``key`` must be ``None`` or a string"
    assert isinstance(language, str), "``language`` must be a string"

    recognize_google(
        encode_falc(audio_data),
        recognizer.operation_timeout,
        key,
        language,
        pfilter,
        show_all,
        with_confidence)

def recognize_google(audio_data:(bytes, str), timeout:float, key=None, language="en-US", pfilter=0, show_all=False, with_confidence=False):
    if key is None:
        key = "AIzaSyBOti4mM-6x9WDnZIjIeyEU21OpBXqWBgw"
    url = f"{server_url_recognize_}?{{}}".format(urlencode({
        "client": "chromium",
        "lang": language,
        "key": key,
        "pFilter": pfilter
    }))
    request = Request(url, data=audio_data[0], headers=audio_data[1])

    # obtain audio transcription results
    response = urlopen(request, timeout=timeout)
    response_text = response.read().decode("utf-8")

    # ignore any blank blocks
    actual_result = []
    for line in response_text.split("\n"):
        if not line: continue
        result = json.loads(line)["result"]
        if len(result) != 0:
            actual_result.append(result[0])

    # return results
    if show_all:
        return actual_result

    if len(actual_result) == 0:
        #or not isinstance(actual_result[0], dict) or len(actual_result.get("alternative", [])) == 0:
        raise UnknownValueError()

    best = {
        "success": False,
        "transcript": "",
        "confidence":  0.0
    }
    for rst in actual_result:
        #for alt in rst["alternative"]:
            alt = rst["alternative"][0] #問答無用に一番上でよい気がする
            if not alt["confidence"]:
                if best["confidence"] < alt["confidence"]:
                    best = {
                        "success": True,
                        "transcript": alt["transcript"],
                        "confidence":  alt["confidence"]
                    }
            else:
                best = {
                    "success": True,
                    "transcript": alt["transcript"],
                    "confidence": 1.0
                }
                break
            # when there is no confidence available, we arbitrarily choose the first hypothesis.
            #best_hypothesis = rst["alternative"][0]
    if not best["success"]:
        raise UnknownValueErrorMod("レスポンスにtranscriptが存在しません", response_text)
        
    return (best["transcript"], best["confidence"], response_text)

class UnknownValueErrorMod(exception.IlluminateException):
    def __init__(self, message: str, raw_data:str, inner: Exception | None = None):
        super().__init__(message, inner)
        self._raw_data = raw_data

    @property
    def raw_data(self):
        return self._raw_data