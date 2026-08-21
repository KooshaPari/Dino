from __future__ import annotations
import time,logging
from collections import defaultdict
from dataclasses import dataclass,field
from typing import Any
logger=logging.getLogger("dinoforge_mcp.agent_metrics")

@dataclass
class AgentMetrics:
    agent_id:str
    invocations:int=0;errors:int=0;total_latency_ms:float=0.0
    tools_used:dict[str,int]=field(default_factory=lambda:defaultdict(int))
    last_invocation:float=0.0
    @property
    def avg_latency_ms(self)->float:return self.total_latency_ms/max(self.invocations,1)
    @property
    def error_rate(self)->float:return self.errors/max(self.invocations,1)
    def to_dict(self)->dict[str,Any]:
        return {"agent_id":self.agent_id,"invocations":self.invocations,"errors":self.errors,"avg_latency_ms":round(self.avg_latency_ms,2),"error_rate":round(self.error_rate,4),"tools_used":dict(self.tools_used)}

class AgentMetricsCollector:
    def __init__(self)->None:
        self._agents:dict[str,AgentMetrics]={}
        self._gi=0;self._ge=0;self._st=time.time()
    def record_invocation(self,aid:str,tool:str,lat:float)->None:
        m=self._e(aid);m.invocations+=1;m.total_latency_ms+=lat;m.tools_used[tool]+=1;m.last_invocation=time.time();self._gi+=1
    def record_error(self,aid:str,tool:str)->None:
        m=self._e(aid);m.errors+=1;m.tools_used[tool]+=1;self._ge+=1
    def get_agent_summary(self,aid:str)->dict[str,Any]:return self._agents[aid].to_dict() if aid in self._agents else {}
    def get_all_agents(self)->list[dict[str,Any]]:return [m.to_dict() for m in self._agents.values()]
    def get_global_summary(self)->dict[str,Any]:
        u=time.time()-self._st
        return {"total_agents":len(self._agents),"total_invocations":self._gi,"total_errors":self._ge,"uptime_seconds":round(u,1)}
    def to_prometheus(self)->str:
        L=["# HELP dinoforge_agent_invocations_total Invocations","# TYPE dinoforge_agent_invocations_total counter"]
        for a,m in self._agents.items():L.append("dinoforge_agent_invocations_total{agent=""+a+""} "+str(m.invocations))
        return chr(10).join(L)
    def _e(self,aid:str)->AgentMetrics:
        if aid not in self._agents:self._agents[aid]=AgentMetrics(agent_id=aid)
        return self._agents[aid]

_col:AgentMetricsCollector|None=None
def get_metrics_collector()->AgentMetricsCollector:
    global _col
    if _col is None:_col=AgentMetricsCollector()
    return _col
