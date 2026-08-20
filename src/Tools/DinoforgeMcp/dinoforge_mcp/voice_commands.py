"""
Module 5: Voice Command & Intent Tools
"""
from __future__ import annotations

import asyncio
import base64
import io
import logging
import os
import re
from typing import Any

from fastmcp import FastMCP, Context

from .config import _run_game_cli, logger

# Intent patterns mapped to tool invocations
VOICE_INTENTS = {
    r'(?:enable|load|activate)\s+(?:the\s+)?(.+?)\s+(?:mod|pack)': 'enable_pack',
    r'(?:disable|unload|deactivate)\s+(?:the\s+)?(.+?)\s+(?:mod|pack)': 'disable_pack',
    r'(?:reload|refresh)\s+(?:all\s+)?mods': 'reload_mods',
    r'(?:take|capture)\s+(?:a\s+)?(?:screenshot|pic)': 'screenshot',
    r'(?:show|get|check)\s+(?:game\s+)?status': 'status',
    r'(?:open|toggle)\s+(?:the\s+)?(?:mods?\s+)?menu': 'open_menu',
    r'(?:open|toggle)\s+(?:the\s+)?debug': 'open_debug',
    r'(?:open|show)\s+(?:the\s+)?(?:mods?\s+)?panel': 'open_menu',
    r'press\s+(?:the\s+)?f(\d+)': 'press_f_key',
}


async def _transcribe_audio_openai(audio_b64: str, language: str = "en-US") -> str | None:
    """
    Transcribe base64-encoded audio via OpenAI Whisper API.

    Returns:
        Transcribed text, or None if transcription fails or API key is missing.
    """
    try:
        from openai import OpenAI

        api_key = os.getenv("OPENAI_API_KEY")
        if not api_key:
            logger.warning("OPENAI_API_KEY not set; voice transcription unavailable")
            return None

        client = OpenAI(api_key=api_key)

        # Decode base64 audio
        audio_bytes = base64.b64decode(audio_b64)
        audio_file = io.BytesIO(audio_bytes)
        audio_file.name = "audio.wav"

        # Call Whisper API
        response = client.audio.transcriptions.create(
            model="whisper-1",
            file=audio_file,
            language=language,
        )

        return response.text
    except ImportError:
        logger.warning("openai package not installed; voice transcription unavailable")
        return None
    except Exception as e:
        logger.warning(f"Whisper transcription failed: {e}")
        return None


async def _match_intent(text: str) -> tuple[str, dict[str, Any]]:
    """
    Match user text against intent patterns and extract parameters.

    Returns:
        (intent_name, parameters_dict)
    """
    text_lower = text.lower().strip()

    for pattern, intent_name in VOICE_INTENTS.items():
        match = re.search(pattern, text_lower)
        if match:
            params = {}

            if intent_name == 'enable_pack':
                # Extract pack name from group 1 (the captured group in regex)
                pack_name = match.group(1).strip().replace(' ', '-').lower()
                params['pack'] = pack_name
            elif intent_name == 'disable_pack':
                pack_name = match.group(1).strip().replace(' ', '-').lower()
                params['pack'] = pack_name
            elif intent_name == 'press_f_key':
                f_key = int(match.group(1))
                params['key_num'] = f_key

            return (intent_name, params)

    # No match — return unknown intent
    return ('unknown', {})


async def _invoke_intent(intent_name: str, params: dict[str, Any], pipe_name: str | None = None) -> dict[str, Any]:
    """
    Invoke the appropriate MCP tool based on intent name and parameters.

    Returns:
        Tool result dict.
    """
    try:
        if intent_name == 'enable_pack':
            pack_name = params.get('pack', '')
            if not pack_name:
                return {'success': False, 'error': 'No pack name extracted from voice command'}
            return _run_game_cli('enable-pack', pack_name, pipe_name=pipe_name)

        elif intent_name == 'disable_pack':
            pack_name = params.get('pack', '')
            if not pack_name:
                return {'success': False, 'error': 'No pack name extracted from voice command'}
            return _run_game_cli('disable-pack', pack_name, pipe_name=pipe_name)

        elif intent_name == 'reload_mods':
            return _run_game_cli('reload-packs', pipe_name=pipe_name)

        elif intent_name == 'screenshot':
            return _run_game_cli('screenshot', pipe_name=pipe_name)

        elif intent_name == 'status':
            return _run_game_cli('status', pipe_name=pipe_name)

        elif intent_name == 'open_menu':
            return _run_game_cli('input', 'F10', pipe_name=pipe_name)

        elif intent_name == 'open_debug':
            return _run_game_cli('input', 'F9', pipe_name=pipe_name)

        elif intent_name == 'press_f_key':
            key_num = params.get('key_num', 0)
            if key_num < 1 or key_num > 12:
                return {'success': False, 'error': f'F{key_num} out of range; expected F1–F12'}
            return _run_game_cli('input', f'F{key_num}', pipe_name=pipe_name)

        else:
            return {'success': False, 'error': f'Unknown intent: {intent_name}'}

    except Exception as e:
        return {'success': False, 'error': f'Intent invocation failed: {e}'}


def register(mcp: FastMCP):
    """Register voice command tools with the MCP server."""

    @mcp.tool()
    async def voice_command(
        ctx: Context,
        audio_b64: str,
        language: str = "en-US",
        pipe_name: str | None = None,
    ) -> dict:
        """
        Control mods via voice command.

        Accepts base64-encoded WAV/MP3 audio, transcribes it via OpenAI Whisper,
        matches intent patterns, and invokes the appropriate game tool.

        Args:
            audio_b64: Base64-encoded WAV or MP3 audio bytes.
            language: Language code for Whisper (e.g., 'en-US', 'en', 'fr'). Default 'en-US'.
            pipe_name: Optional named pipe name for game bridge.

        Returns:
            dict with keys: success, transcription, intent, result.
            result contains the output of the invoked tool (or error if no intent matched).
        """
        # Transcribe
        transcription = await _transcribe_audio_openai(audio_b64, language)
        if not transcription:
            return {
                'success': False,
                'transcription': None,
                'intent': None,
                'error': 'Audio transcription failed — check OPENAI_API_KEY env var'
            }

        # Match intent
        intent_name, params = await _match_intent(transcription)

        # Invoke
        result = await _invoke_intent(intent_name, params, pipe_name=pipe_name)

        return {
            'success': result.get('success', False),
            'transcription': transcription,
            'intent': intent_name if intent_name != 'unknown' else None,
            'result': result,
            'error': result.get('error') if not result.get('success') else None,
        }

    @mcp.tool()
    async def voice_command_intent(
        ctx: Context,
        text: str,
        pipe_name: str | None = None,
    ) -> dict:
        """
        Control mods via text intent (no speech recognition).

        Accepts plain text, matches intent patterns, and invokes the appropriate tool.
        Useful for chat-style interaction or testing without audio.

        Args:
            text: User command text (e.g., 'enable star wars mod', 'take screenshot').
            pipe_name: Optional named pipe name for game bridge.

        Returns:
            dict with keys: success, intent, result.
        """
        # Match intent
        intent_name, params = await _match_intent(text)

        # Invoke
        result = await _invoke_intent(intent_name, params, pipe_name=pipe_name)

        return {
            'success': result.get('success', False),
            'input_text': text,
            'intent': intent_name if intent_name != 'unknown' else None,
            'result': result,
            'error': result.get('error') if not result.get('success') else None,
        }
