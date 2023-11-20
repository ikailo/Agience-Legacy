from technologai_agent.information import Information
from technologai_agent.template import Template

class ShowMessageToUser(Template):
    def __init__(self, message_callback, **kwargs):
        super().__init__(
            id="show_message_to_user",
            description="Show a message to the user.",
            **kwargs
        )
        self.message_callback = message_callback

    async def assess(self, information: Information):
        return True

    async def process(self, information: Information):
        self.message_callback(information.input if information.input else '')
        return None