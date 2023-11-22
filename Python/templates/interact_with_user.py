from agience_agent.information import Information
from agience_agent.template import Template

class InteractWithUser(Template):
    def __init__(self, **kwargs):
        super().__init__(
            id="interact_with_user",
            description="Show a message to the user and then receive a text input from the user. Find, and then respond with, the best template response to the user's input.",
            **kwargs
        )

    async def assess(self, information: Information):
        return True

    async def process(self, information: Information):
        await information.publish("show_message_to_user", f"{information.input} \n> ")

        user_input = await information.publish("get_input_from_user")

        if user_input and user_input["message"].startswith("DEBUG:"):
            return await information.publish("debug", user_input["message"])

        best_template = await information.publish("get_best_template", user_input["message"])

        return await information.publish(best_template.structured.get("id", "input_to_output"), user_input)