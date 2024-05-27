import asyncio
from templates import GetInputFromUser, InteractWithUser, Debug, ShowMessageToUser
from agience_agent.agent import Agent

import dotenv
import os
import traceback


class Program:
    agent = None
    is_started = True

    @staticmethod
    async def main():
        dotenv.load_dotenv()
        auth_uri = os.getenv("AUTH_URI")
        client_id = os.getenv("CLIENT_ID")
        client_secret = os.getenv("CLIENT_SECRET")
        member_id = os.getenv("MEMBER_ID")

        try:
            print("Loading...")

            Program.agent = Agent(
                auth_uri,
                client_id,
                client_secret,
                member_id,
                log_message_callback=Program.log_message_callback
            )

            # This is kinda weird, but this way we can use the template IDs as keys in the catalog, and also we can pass the agent itself to the templates (in this case, just Debug)
            # In contrast, in the C# code, there is a Catalog class with an add function that is used after Agent initialization.
            # TODO there is probably a much cleaner way to do this
            get_input_from_user = GetInputFromUser(member_id=member_id)
            interact_with_user = InteractWithUser(member_id=member_id)
            debug = Debug(Program.agent, member_id=member_id)
            show_message_to_user = ShowMessageToUser(Program.show_message_to_user_callback, member_id=member_id)
            Program.agent.catalog = {
                get_input_from_user.id: get_input_from_user,
                interact_with_user.id: interact_with_user,
                debug.id: debug,
                show_message_to_user.id: show_message_to_user
            }

            await Program.agent.start()

            await Program.agent.publish_async(
                template_id="interact_with_user",
                input_data="Ready for Input",
                callback=Program.interact_with_user_callback
            )

            while Program.is_started:
                await asyncio.sleep(0.01)

            await Program.agent.stop()

        except Exception:
            print(traceback.format_exc())

    @staticmethod
    async def interact_with_user_callback(output):
        print("interact_with_user_callback")
        if output and output.raw.lower() == "quit":
            Program.is_started = False
            print(f"{Program.agent.name if Program.agent else 'Agience.Console'} Shutting Down")
        else:
            await Program.agent.publish_async("interact_with_user", Program.interact_with_user_callback, output)

    @staticmethod
    def show_message_to_user_callback(message):
        print("show_message_to_user_callback")
        print(message if message else "")

    @staticmethod
    def log_message_callback(sender, message):
        print(f"{Program.agent.name if Program.agent else 'Interaction.Local'} | {message}")


if __name__ == "__main__":
    asyncio.run(Program.main())
