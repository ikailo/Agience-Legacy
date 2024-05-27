from typing import List, Optional
from .information import Information

class Template:
    def __init__(self, id=None, description=None, input_keys=None, output_keys=None, member_id=None):
        self.id = id
        self.description = description
        self.input_keys = input_keys
        self.output_keys = output_keys
        self.member_id = member_id

    async def assess(self, information: Information) -> bool:
        return False

    async def process(self, information: Information) -> Optional[dict]:
        return None