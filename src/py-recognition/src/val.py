"""
定数モジュール
"""

def __support_wisper() -> bool:
    try:
        import whisper # type: ignore
    except:
        pass
    else:
        return True

    try:
        import faster_whisper # type: ignore
    except:
        return False
    else:
        return True


def __default_method_value() -> str:
    try:
        import faster_whisper # type: ignore
    except:
        return METHOD_VALUE_GOOGLE_DUPLEX
    else:
        return METHOD_VALUE_WHISPER_FASTER


def __choice_method() -> list[str]:
    r = []
    try:
        import whisper # type: ignore
    except:
        pass
    else:
        r.append(METHOD_VALUE_WHISPER)
    try:
        import faster_whisper # type: ignore
    except:
        pass
    else:
        r.append(METHOD_VALUE_WHISPER_FASTER)
    r.append(METHOD_VALUE_GOOGLE)
    r.append(METHOD_VALUE_GOOGLE_DUPLEX)
    return r

SUPPORT_WISPER = __support_wisper()

VERBOSE_MIN = 0
VERBOSE_INFO = 1
VERBOSE_DEBUG = 2
VERBOSE_TRACE = 3

ARG_NAME_VERBOSE = "--verbose"
ARG_NAME_LOG_FILE = "--log_file"
ARG_NAME_LOG_DIRECTORY = "--log_directory"
ARG_NAME_LOG_ROTATE= "--log_rotate"

ARG_DEFAULT_VERBOSE = str(VERBOSE_INFO)
ARG_DEFAULT_LOG_FILE = "recognize.log"
ARG_DEFAULT_LOG_DIRECTORY = None

ARG_CHOICE_VERBOSE = list(map(lambda x: str(x), [
    VERBOSE_MIN,
    VERBOSE_INFO,
    VERBOSE_DEBUG,
#    VERBOSE_TRACE
    ]))

TEST_VALUE_RECOGNITION = "recognition"
TEST_VALUE_MIC = "mic"
TEST_VALUE_AMBIENT= "mic_ambient"
ARG_CHOICE_TEST = [
    "",
    TEST_VALUE_RECOGNITION,
    TEST_VALUE_MIC,
    TEST_VALUE_AMBIENT
]

METHOD_VALUE_WHISPER = "whisper"
METHOD_VALUE_WHISPER_FASTER = "faster_whisper"
METHOD_VALUE_GOOGLE= "google"
METHOD_VALUE_GOOGLE_DUPLEX = "google_duplex"
DEFALUT_METHOD_VALUE = __default_method_value()
ARG_CHOICE_METHOD = __choice_method()

OUT_VALUE_PRINT = "print"
OUT_VALUE_YUKARINETTE = "yukarinette"
OUT_VALUE_YUKACONE = "yukacone"
OUT_VALUE_ILLUMINATE= "illuminate"
ARG_CHOICE_OUT = [
    OUT_VALUE_PRINT,
    OUT_VALUE_YUKARINETTE,
    OUT_VALUE_YUKACONE,
    OUT_VALUE_ILLUMINATE,
]

LANGUAGE_CODES = [
    "",
    "af",
    "am",
    "ar",
    "as",
    "az",
    "ba",
    "be",
    "bg",
    "bn",
    "bo",
    "br",
    "bs",
    "ca",
    "cs",
    "cy",
    "da",
    "de",
    "el",
    "en",
    "es",
    "et",
    "eu",
    "fa",
    "fi",
    "fo",
    "fr",
    "gl",
    "gu",
    "ha",
    "haw",
    "he",
    "hi",
    "hr",
    "ht",
    "hu",
    "hy",
    "id",
    "is",
    "it",
    "ja",
    "jw",
    "ka",
    "kk",
    "km",
    "kn",
    "ko",
    "la",
    "lb",
    "ln",
    "lo",
    "lt",
    "lv",
    "mg",
    "mi",
    "mk",
    "ml",
    "mn",
    "mr",
    "ms",
    "mt",
    "my",
    "ne",
    "nl",
    "nn",
    "no",
    "oc",
    "pa",
    "pl",
    "ps",
    "pt",
    "ro",
    "ru",
    "sa",
    "sd",
    "si",
    "sk",
    "sl",
    "sn",
    "so",
    "sq",
    "sr",
    "su",
    "sv",
    "sw",
    "ta",
    "te",
    "tg",
    "th",
    "tk",
    "tl",
    "tr",
    "tt",
    "uk",
    "ur",
    "uz",
    "vi",
    "yi",
    "yo",
    "zh",
]