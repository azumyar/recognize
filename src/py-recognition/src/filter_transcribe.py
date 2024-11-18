import re
from typing import Any, Callable, Iterable, Optional, NamedTuple

import src.exception as ex

RULE_MATCH = "match"
RULE_MATCH_ALL = "match-all"
RULE_REGEX = "regex"

ACTION_MASK = "mask"
ACTION_MASK_ALL = "mask-all"
ACTION_REPLACE = "replace"

class TranscribeFilterRuleSet(NamedTuple):
    rule:str
    action:str
    src:str
    dst:str


class TranscribeFilterSet(NamedTuple):
    name:str
    enabled:bool
    rules:list[TranscribeFilterRuleSet]



class JsonReadException(ex.IlluminateException):
    """
    JSON読み込みに失敗
    """
    pass


class TranscribeFilter:
    def __init__(self, json:Any):
        self.__filter_set:list[TranscribeFilterSet] = []
        if json is not None:
            if not "filters" in json:
                raise JsonReadException("設定JSONにfiltersが定義されていません")

            for i in range(len(json["filters"])):
                fil = json["filters"][i]
                for check in ["name", "enable", "rules"]:
                    if not check in fil:
                        raise JsonReadException(f"フィルタセット[{i}]に{check}が定義されていません")
                if fil["enable"]:
                    rules:list[TranscribeFilterRuleSet] = []
                    for j in range(len(fil["rules"])):
                        rul = fil["rules"][j]
                        for check in ["rule", "action", "src", "dst"]:
                            if not check in rul:
                                raise JsonReadException(f"フィルタセット[{i}]ルール[{j}]に{check}が定義されていません")
                        if not rul["rule"] in [RULE_MATCH, RULE_MATCH_ALL, RULE_REGEX]:
                            raise JsonReadException(f"フィルタセット[{i}]ルール[{j}]の定義[rule: {rul['rule']}]は無効です")
                        if not rul["action"] in [ACTION_MASK, ACTION_MASK_ALL, ACTION_REPLACE]:
                            raise JsonReadException(f"フィルタセット[{i}]ルール[{j}]の定義[action: {rul['action']}]は無効です")
                            

                        rules.append(TranscribeFilterRuleSet(
                            rul["rule"],
                            rul["action"],
                            rul["src"],
                            rul["dst"]))
                    self.__filter_set.append(TranscribeFilterSet(
                        fil["name"],
                        fil["enable"],
                        rules))
    
    @property
    def has_rule(self) -> bool:
        return 0 < len(self.__filter_set)

    def filter(self, text:str) -> str:
        result = text
        for set in self.__filter_set:
            if set.enabled:
                for rule in set.rules:
                    if rule.src == "":
                        continue
                    f = {
                        ACTION_MASK: self.action_mask,
                        ACTION_MASK_ALL: self.action_mask_all,
                        ACTION_REPLACE: self.action_replace,
                    }[rule.action]
                    p, r = f(result, rule)
                    if p:
                        return r
        return result

    def action_mask(self, text:str, rule_set:TranscribeFilterRuleSet) -> tuple[bool, str]:
        if rule_set.rule == RULE_MATCH:
            mask = rule_set.src[0] + (rule_set.dst * (len(rule_set.src) - 1))
            r = text.replace(rule_set.src, mask)
            return (r != text, r)
        elif rule_set.rule == RULE_MATCH_ALL:
            if text == rule_set.src:
                return (True, rule_set.src[0] + (rule_set.dst * (len(text) - 1)))
            else:
                return (False, text)
        elif rule_set.rule == RULE_REGEX:
            r = re.sub(
                rule_set.src,
                lambda m: m.group(0)[0] + (rule_set.dst * (len(m.group(0)) - 1)),
                text)
            return (r != text, r)
        raise RuntimeError(f"不明なルール：{rule_set.rule}")

    def action_mask_all(self, text:str, rule_set:TranscribeFilterRuleSet) -> tuple[bool, str]:
        if rule_set.rule == RULE_MATCH:
            mask = rule_set.dst * len(rule_set.src)
            r = text.replace(rule_set.src, mask)
            return (r != text, r)
        elif rule_set.rule == RULE_MATCH_ALL:
            if text == rule_set.src:
                return (True, rule_set.dst * len(text))
            else:
                return (False, text)
        elif rule_set.rule == RULE_REGEX:
            r = re.sub(
                rule_set.src,
                lambda m: rule_set.dst * len(m.group(0)),
                text)
            return (r != text, r)
        raise RuntimeError(f"不明なルール：{rule_set.rule}")

    def action_replace(self, text:str, rule_set:TranscribeFilterRuleSet) -> tuple[bool, str]:
        if rule_set.rule == RULE_MATCH:
            r = text.replace(rule_set.src, rule_set.dst)
            return (r != text, r)
        elif rule_set.rule == RULE_MATCH_ALL:
            if text == rule_set.src:
                return (True, rule_set.dst)
            else:
                return (False, text)
        elif rule_set.rule == RULE_REGEX:
            r = re.sub(rule_set.src, rule_set.dst, text)
            return (r != text, r)
        raise RuntimeError(f"不明なルール：{rule_set.rule},{RULE_MATCH_ALL}")



