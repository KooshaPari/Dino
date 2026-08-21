from __future__ import annotations
import hashlib,os,secrets,time,logging
from dataclasses import dataclass,field
from typing import Any
logger=logging.getLogger("dinoforge_mcp.agent_auth")

@dataclass(frozen=True)
class AuthResult:
    valid:bool
    agent_id:str=""
    scopes:list[str]=field(default_factory=list)
    expires_at:float|None=None
    error:str|None=None

@dataclass
class AgentKey:
    key_id:str;key_hash:str;agent_id:str;scopes:list[str]
    created_at:float=field(default_factory=time.time);expires_at:float|None=None;is_revoked:bool=False

class AgentAuth:
    DEFAULT_SCOPES=["read","write"]
    ALL_SCOPES=["read","write","admin","game_control","asset_pipeline"]
    def __init__(self,secret_key:str|None=None)->None:
        self._key=secret_key or os.environ.get("DINOFORGE_SECRET_KEY","dev")
        self._keys:dict[str,AgentKey]={}
    def register_key(self,agent_id:str,scopes:list[str]|None=None,expires_in_seconds:int|None=None)->str:
        raw=secrets.token_urlsafe(32)
        h=hashlib.sha256(raw.encode()).hexdigest()
        exp=time.time()+expires_in_seconds if expires_in_seconds else None
        kid=secrets.token_hex(8)
        self._keys[kid]=AgentKey(key_id=kid,key_hash=h,agent_id=agent_id,scopes=scopes or self.DEFAULT_SCOPES,expires_at=exp)
        return raw
    def authenticate(self,api_key:str)->AuthResult:
        if not api_key:return AuthResult(valid=False,error="No key")
        h=hashlib.sha256(api_key.encode()).hexdigest()
        for k in self._keys.values():
            if k.key_hash==h:
                if k.is_revoked:return AuthResult(valid=False,agent_id=k.agent_id,error="Revoked")
                if k.expires_at and time.time()>k.expires_at:return AuthResult(valid=False,agent_id=k.agent_id,error="Expired")
                return AuthResult(valid=True,agent_id=k.agent_id,scopes=k.scopes,expires_at=k.expires_at)
        return AuthResult(valid=False,error="Invalid key")
    def revoke_key(self,key_id:str)->bool:
        if key_id in self._keys:self._keys[key_id].is_revoked=True;return True
        return False
    def has_scope(self,auth:AuthResult,scope:str)->bool:
        return auth.valid and not auth.is_expired and scope in auth.scopes
    def list_keys(self)->list[dict[str,Any]]:
        return [{"key_id":k.key_id,"agent_id":k.agent_id,"scopes":k.scopes,"is_revoked":k.is_revoked} for k in self._keys.values()]
    @staticmethod
    def _hash_key(raw:str)->str:return hashlib.sha256(raw.encode()).hexdigest()

_auth:AgentAuth|None=None
def get_agent_auth()->AgentAuth:
    global _auth
    if _auth is None:_auth=AgentAuth()
    return _auth
