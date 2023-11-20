from collections import defaultdict

class Context:
    def __init__(self):
        self._library = {}
        self._templates = {}
        self._forward_context = defaultdict(list)
        self._reverse_context = defaultdict(list)
        self._lineage = {}

    def get_publisher(self, forward_id):
        return self._library.get(self._lineage.get(forward_id))

    def spawn(self, forward_id, reverse_id):
        self.add_forward(forward_id, reverse_id)
        self.add_reverse(forward_id, reverse_id)
        self._lineage[forward_id] = reverse_id

    def add(self, information):
        self._library[information.id] = information

    def add_reverse(self, forward_id, reverse_id):
        self._reverse_context[forward_id].append(reverse_id)

    def add_forward(self, forward_id, reverse_id):
        self._forward_context[reverse_id].append(forward_id)