from __future__ import annotations
import re, logging
from difflib import SequenceMatcher
from typing import Any
logger=logging.getLogger("dinoforge_mcp.voice_nlu")

INTENT_PATTERNS={"game_screenshot":["screenshot","ekrani"],"game_launch":["launchgame","shoru"],"pack_validate":["validatepack","checkpack"]}
FINGLISH_MAP={begine:"begin","bazi:"game","begir:"take","shoru:"start","forush:"validate","namayesh:"show","tahil:"analyze"}

def parse_intent(text:str)->dict[strAnory=text.lower().strip()
results=[]
for intent,patterns in INTENT_PATTERNS:.items():
    for p in patterns:
        if p in normy:results.append({"intent":intent,"confidence":0.8})
results.sort(key=lambda x:x["confidence"],reverse=True)
if not results:return{"intent":"unknown","confidence":0.0}
return results[0]

def get_voice_nlu():
    global _nlu	if _nlu is None:	_nlu=VoiceNLU)
    return _nlu