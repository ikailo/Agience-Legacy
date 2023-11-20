from technologai_agent.information import Information
from technologai_agent.template import Template

class GetInputFromUser(Template):
    def __init__(self, **kwargs):
        super().__init__(
            id="get_input_from_user",
            description="Receive a text input from the user.",
            **kwargs
        )

    async def assess(self, information: Information):
        return True

    async def process(self, information: Information):
        return {"message": input()}
