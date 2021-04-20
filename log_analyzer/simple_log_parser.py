import re
from collections import defaultdict

DEFAULT_ERROR_HEAD = r'(\d\d\d\d-\d\d-\d\d) (\d\d:\d\d:\d\d,\d\d\d \[\d+\] )(ERROR|WARN|FATAL)'


class SimpleLogParser:
    def __init__(self, log_path: str, error_head: str = DEFAULT_ERROR_HEAD):
        self.log_path = log_path
        self.error_head = error_head

    def errors_count(self, from_date: str):
        message_stats = defaultdict(int)
        level_stats = defaultdict(int)

        with open(self.log_path, 'r') as file:
            line_nom = 0
            aggregate_flag = False

            while True:
                line = file.readline()
                line_nom += 1

                if not line:
                    break

                match  = re.search(self.error_head, line, re.IGNORECASE)
                if match:
                    date = match.group(1)
                    level = match.group(3)
                    if date >= from_date:
                        aggregate_flag = True
                        level_stats[level] += 1
                elif aggregate_flag:
                    message_stats[line[:250]] += 1
                    aggregate_flag = False

        return level_stats, message_stats

