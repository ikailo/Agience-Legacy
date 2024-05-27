from typing import Dict, Optional
# from .agent import Agent
from enum import Enum

class InformationState(Enum):
    DRAFT = 0
    OPEN = 1
    CLOSED = 2

class TemplateState(Enum):
    RESTING = 0
    ASSESSING = 1
    PROCESSING = 2

class Information:
    def __init__(self, agent, template_id: str, input_data = None):
        self.agent = agent
        self.id = None  # This should be generated uniquely
        self.creator_id = agent.identity.member_id  # This should be set appropriately
        self.worker_id = None
        self.template_id = template_id
        self.information_state = InformationState.OPEN  # This should be an enum in a real application
        self.template_state = TemplateState.RESTING  # This should be an enum in a real application
        self.input = input_data if input_data else {}
        self.output = {}
        self.assessment_queued = False

    async def assess(self):
        if self.assessment_queued or self.template_state == TemplateState.PROCESSING or self.information_state == InformationState.CLOSED:
            return

        if self.template_id in self.agent.catalog and self.template_state == TemplateState.RESTING:
            self.template_state = TemplateState.ASSESSING
            self.assessment_queued = True
            result = await self.agent.catalog[self.template_id].assess(self)
            self.template_state = TemplateState.RESTING
            return result
        
        self.assessment_queued = False
        return False

    async def process(self):
        if self.template_state == TemplateState.RESTING:
            self.template_state = TemplateState.PROCESSING
            self.output = await self.agent.catalog[self.template_id].process(self)
            self.template_state = TemplateState.RESTING
            self.information_state = InformationState.CLOSED
            await self.agent.publish_async(information=self)  # Send the closed information back to the agent

    async def publish(self, template_id, input_data=None):
        new_info = Information(self.agent, template_id, input_data)
        self.agent.context.add(new_info)
        self.agent.context.spawn(new_info.id, self.id)
        return await self.agent.publish(new_info)

    def __lt__(self, other):
        return self.id < other.id
