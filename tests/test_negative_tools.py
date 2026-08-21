"""Negative and edge-case tests for DINOForge MCP tools."""
import pytest
from unittest.mock import AsyncMock, MagicMock, patch


class TestNegativeToolCalls:
    """Test error handling and edge cases for MCP tools."""

    @pytest.mark.asyncio
    async def test_game_screenshot_with_invalid_path(self):
        with patch("dinoforge_mcp.game_control._pipe_exists", return_value=False):
            from dinoforge_mcp.game_control import game_screenshot
            with pytest.raises(Exception):
                await game_screenshot(output_path="//nonexistent/pipe")

    @pytest.mark.asyncio
    async def test_pack_validate_nonexistent_pack(self):
        from dinoforge_mcp.asset_pipeline import pack_validate
        result = await pack_validate(pack_path="/nonexistent/path/pack.yaml")
        assert result is not None

    @pytest.mark.asyncio
    async def test_catalog_list_empty(self):
        with patch("dinoforge_mcp.catalog_inspection._load_catalog", return_value={}):
            from dinoforge_mcp.catalog_inspection import catalog_list
            result = await catalog_list()
            assert result is not None

    @pytest.mark.asyncio
    async def test_log_analyze_empty_file(self):
        import tempfile, os
        fd, path = tempfile.mkstemp(suffix='.log')
        os.close(fd)
        try:
            from dinoforge_mcp.log_analysis import log_analyze
            result = await log_analyze(log_path=path, pattern="ERROR")
            assert result is not None
        finally:
            os.unlink(path)

    @pytest.mark.asyncio
    async def test_voice_recognize_empty_audio(self):
        from dinoforge_mcp.voice_commands import voice_recognize
        result = await voice_recognize(audio_path="")
        assert result is not None

    @pytest.mark.asyncio
    async def test_rate_limiter_allows_first_request(self):
        from dinoforge_mcp.rate_limiter import TokenBucketLimiter
        limiter = TokenBucketLimiter(rate=10, capacity=10)
        assert limiter.allow("agent-1") is True

    @pytest.mark.asyncio
    async def test_rate_limiter_blocks_burst(self):
        from dinoforge_mcp.rate_limiter import TokenBucketLimiter
        limiter = TokenBucketLimiter(rate=1, capacity=2)
        for _ in range(2):
            limiter.allow("agent-1")
        assert limiter.allow("agent-1") is False

    def test_audit_logger_records_events(self):
        from dinoforge_mcp.audit_logger import AuditLogger
        al = AuditLogger(service="test")
        al.tool_invocation("test_tool", actor="test-agent")
        recent = al.get_recent()
        assert len(recent) == 1
        assert recent[0]["event_type"] == "tool_invocation"

    def test_audit_logger_stats(self):
        from dinoforge_mcp.audit_logger import AuditLogger
        al = AuditLogger(service="test")
        al.tool_invocation("tool-a")
        al.tool_invocation("tool-b")
        al.authentication("user", "token")
        stats = al.get_stats()
        assert stats["tool_invocation"] == 2
        assert stats["authentication"] == 1

    def test_rate_limiter_separate_agents(self):
        from dinoforge_mcp.rate_limiter import TokenBucketLimiter
        limiter = TokenBucketLimiter(rate=1, capacity=1)
        limiter.allow("agent-1")
        assert limiter.allow("agent-2") is True

    @pytest.mark.asyncio
    async def test_mcp_server_health_endpoint(self):
        from dinoforge_mcp.api_rest import app
        from fastapi.testclient import TestClient
        client = TestClient(app)
        response = client.get("/health")
        assert response.status_code == 200
        assert response.json()["status"] == "healthy"

    @pytest.mark.asyncio
    async def test_mcp_server_tools_list(self):
        from dinoforge_mcp.api_rest import app
        from fastapi.testclient import TestClient
        client = TestClient(app)
        response = client.get("/tools")
        assert response.status_code == 200
        assert isinstance(response.json(), list)

    @pytest.mark.asyncio
    async def test_mcp_server_unknown_tool(self):
        from dinoforge_mcp.api_rest import app
        from fastapi.testclient import TestClient
        client = TestClient(app)
        response = client.post("/tools/nonexistent-tool", json={"args": {}})
        assert response.status_code == 404
