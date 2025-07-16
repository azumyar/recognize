"""
定数モジュール
"""
from enum import Enum
import importlib.util
import ctypes


def __is_available_cuda():
    try:
        cuda_device_count = ctypes.c_uint32()
        cuda_device_count.value = 0

        _nvcuda = ctypes.WinDLL("nvcuda.dll")
        _nvcuda.cuInit.restype = ctypes.c_int32
        _nvcuda.cuDeviceGetCount.argtypes = (ctypes.c_uint32,)
        _nvcuda.cuDeviceGetCount.restype = ctypes.c_int32
        _nvcuda.cuDeviceGetCount.argtypes = (ctypes.POINTER(ctypes.c_uint32),)
        _nvcuda.cuInit(0)
        _nvcuda.cuDeviceGetCount(cuda_device_count)
        return 0 < cuda_device_count.value
    except:
        return False

def __support_whisper() -> bool:
    if importlib.util.find_spec("whisper") is None:
        return False
    else:
        return True

def __support_whisper_faster() -> bool:
    if importlib.util.find_spec("faster_whisper") is None:
        return False
    else:
        return True

def __support_whisper_kotoba() -> bool:
    if importlib.util.find_spec("transformers") is None:
        return False
    elif importlib.util.find_spec("torch") is None:
        return False
    else:
        return True


def __default_method_value() -> str:
    if __support_whisper_faster():
        return METHOD_VALUE_WHISPER_FASTER
    else:
        return METHOD_VALUE_GOOGLE_DUPLEX


def __choice_method() -> list[str]:
    r = []
    if SUPPORT_LIB_WHISPER:
        r.append(METHOD_VALUE_WHISPER)
    if SUPPORT_LIB_WHISPER_FASTER:
        r.append(METHOD_VALUE_WHISPER_FASTER)
    if SUPPORT_LIB_WHISPER_KOTOBA:
        r.append(METHOD_VALUE_WHISPER_KOTOBA)

    return r + [METHOD_VALUE_GOOGLE, METHOD_VALUE_GOOGLE_DUPLEX, METHOD_VALUE_GOOGLE_MIX]

def __choice_translate() -> list[str]:
    r = [ "" ]
    if SUPPORT_LIB_WHISPER_KOTOBA:
        r.append(METHOD_VALUE_WHISPER_KOTOBA)
    return r

def __support_silero_vad() -> bool:
    if importlib.util.find_spec("torch") is None:
        return False
    else:
        return SUPPORT_CUDA

def __choice_vad() -> list[str]:
    r = [ VAD_VALUE_GOOGLE ]
    if __support_silero_vad():
        r.append(VAD_VALUE_SILERO)

    return r

class Console(Enum):
    Bold = "\033[1m"
    """太字"""
    UnderLine = "\033[4m"
    """下線"""
    Invisible = "\033[08m"
    """不可視"""

    Black = "\033[30m"
    """(文字色)黒"""
    Red = "\033[31m"
    """(文字色)赤"""
    Green = "\033[32m"
    """(文字色)緑"""
    Yellow = "\033[33m"
    """(文字色)黄"""
    Blue = "\033[34m"
    """(文字色)青"""
    Magenta = "\033[35m"
    """(文字色)マゼンタ"""
    Cyan = "\033[36m"
    """(文字色)シアン"""
    White = "\033[37m"
    """(文字色)白"""
    DefaultColor  = "\033[39m"
    """デフォルト文字色"""

    BackgroundBlack = "\033[40m"
    """(背景色)黒"""
    BackgroundRed = "\033[41m"
    """(背景色)赤"""
    BackgroundGreen = "\033[42m"
    """(背景色)緑"""
    BackgroundYellow = "\033[43m"
    """(背景色)黄"""
    BackgroundBlue = "\033[44m"
    """(背景色)青"""
    BackgroundMagenta = "\033[45m"
    """(背景色)マゼンタ"""
    BackgroundCyan = "\033[46m"
    """(背景色)シアン"""
    BackgroundWhite = "\033[47m"
    """(背景色)白"""
    BackgroundDefaultColor = "\033[49m"
    """背景色をデフォルトに戻す"""
    Reset = "\033[0m"
    """全てリセット"""

    ReverseColor = "\033[07m"
    """文字色と背景色を反転"""

    @staticmethod
    def foreground(r:int, g:int, b:int) -> str:
        return f"\033[38;2;{r};{g};{b}m"
    @staticmethod
    def foreground_index(index:int) -> str:
        return f"\033[38;5;{index}m"
    @staticmethod
    def background(r:int, g:int, b:int) -> str:
        return f"\033[48;2;{r};{g};{b}m"
    @staticmethod
    def background_index(index:int) -> str:
        return f"\033[48;5;{index}m"

SUPPORT_CUDA = __is_available_cuda()
SUPPORT_LIB_WHISPER = __support_whisper()
SUPPORT_LIB_WHISPER_FASTER = __support_whisper_faster()
SUPPORT_LIB_WHISPER_KOTOBA = __support_whisper_kotoba()
SUPPORT_WHISPER = SUPPORT_LIB_WHISPER or SUPPORT_LIB_WHISPER_FASTER or SUPPORT_LIB_WHISPER_KOTOBA

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
    VERBOSE_TRACE
    ]))

TEST_VALUE_MIC = "mic"
TEST_VALUE_AMBIENT= "mic_ambient"
TEST_VALUE_ILLUMINATE= "illuminate"
ARG_CHOICE_TEST = [
    "",
    TEST_VALUE_MIC,
    TEST_VALUE_AMBIENT,
    TEST_VALUE_ILLUMINATE,
]

METHOD_VALUE_WHISPER = "whisper"
METHOD_VALUE_WHISPER_FASTER = "faster_whisper"
METHOD_VALUE_WHISPER_KOTOBA = "kotoba_whisper"
METHOD_VALUE_GOOGLE= "google"
METHOD_VALUE_GOOGLE_DUPLEX = "google_duplex"
METHOD_VALUE_GOOGLE_MIX = "google_mix"
DEFALUT_METHOD_VALUE = __default_method_value()
ARG_CHOICE_METHOD = __choice_method()

ARG_CHOICE_TRANSLATE = __choice_translate()

SUBTITLE_VALUE_FILE = "file"
SUBTITLE_VALUE_OBS_WS_V5 = "obs"
ARG_CHOICE_SUBTITLE = [
    "",
    SUBTITLE_VALUE_FILE,
    SUBTITLE_VALUE_OBS_WS_V5,
]


MIC_API_VALUE_MME = "mme"
MIC_API_VALUE_WASAPI = "wasapi"
ARG_CHOICE_MIC_API = [
    MIC_API_VALUE_MME,
    MIC_API_VALUE_WASAPI,
]
MIC_SAMPLE_RATE = 16000
MIC_SAMPLE_WIDTH = 2

VAD_VALUE_GOOGLE = "google"
VAD_VALUE_SILERO = "silero"
ARG_CHOICE_VAD = __choice_vad()

OUT_VALUE_PRINT = "print"
OUT_VALUE_YUKARINETTE = "yukarinette"
OUT_VALUE_YUKACONE = "yukacone"
OUT_VALUE_ILLUMINATE= "illuminate"
OUT_VALUE_OBS= "obs"
OUT_VALUE_FILE= "file"
OUT_VALUE_VRC= "vrc"
ARG_CHOICE_OUT = [
    OUT_VALUE_PRINT,
    OUT_VALUE_YUKARINETTE,
    OUT_VALUE_YUKACONE,
    OUT_VALUE_ILLUMINATE,
    OUT_VALUE_OBS,
    OUT_VALUE_FILE,
    OUT_VALUE_VRC,
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
