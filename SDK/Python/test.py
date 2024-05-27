class ExampleClass:
    def __init__(self, callback):
        self.callback = callback

    def trigger_callback(self):
        self.callback()

class Agent:
    def __init__(self, message):
        self.message = message
        self.example = ExampleClass(self.agent_callback)

    def agent_callback(self):
        print(self.message)

agent = Agent("Testing")
agent.example.trigger_callback()