from technologai_agent.information import Information
from technologai_agent.template import Template

import json

class Debug(Template):
    def __init__(self, agent, **kwargs):
        super().__init__(id="debug", description="Debug", **kwargs)
        self._agent = agent

    async def assess(self, information: Information):
        return True

    # TODO in the C# code, there is a IF debug, but I am not sure if Python has an equivalent
    async def process(self, information: Information):
        # Parse the input for the template id and user data
        first_space = information.input.find(' ')

        if first_space > 6:
            template_id = information.input[6:first_space]
            user_data = information.input[first_space + 1:]

            if not template_id or template_id not in self._agent.catalog or not user_data:
                return None

            template = self._agent.catalog[template_id]

            # if template.input_keys and len(template.input_keys) > 0:
            #     data = Data(user_data, DataFormat.STRUCTURED)
            # else:
            #     data = Data(user_data, DataFormat.RAW)
            if template.input_keys and len(template.input_keys) == 0:
                user_data = json.loads(user_data)

            return await information.publish(template_id, user_data)
        else:
            # TODO: Allow parameterless debug with no data
            return "Invalid debug command"
