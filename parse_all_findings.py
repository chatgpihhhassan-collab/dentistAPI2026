import re
import json

with open('report_dentia_full.txt', 'r', encoding='utf-8') as f:
    text = f.read()

pages = text.split('==================== PAGE ')

# Summary table from pages 8-11
summary_text = '\n'.join(pages[8:11])
all_40_matches = re.findall(r'([A-Z]{2,4}-\s*\d{3})', summary_text)
all_40_cleaned = []
for m in all_40_matches:
    cleaned = re.sub(r'\s+', '', m)
    if cleaned not in all_40_cleaned:
        all_40_cleaned.append(cleaned)

print(f"Total in summary table: {len(all_40_cleaned)}")

# Now parse all finding details across pages 11-47
# A finding begins with line: (ID) · (Title) or (ID) - (Title)
findings_details = {}
all_detail_text = '\n==================== PAGE '.join(pages[11:])

# Regex matching: ([A-Z]{2,4}-\d{3}) · (Title)
finding_splits = re.split(r'\n(?=([A-Z]{2,4}-\d{3})\s*[·\-\–\—])', all_detail_text)

# Let's inspect splits
current_id = None
parsed_findings = {}

# More robust regex: find all occurrences of pattern:
# r'([A-Z]{2,4}-\d{3})\s*[·\-\–\—]\s*([^\n]+)'
pattern = re.compile(r'([A-Z]{2,4}-\d{3})\s*[·\-\–\—]\s*([^\n]+)')
matches = list(pattern.finditer(all_detail_text))

print(f"Found {len(matches)} finding headers across pages 11-47")

for idx, m in enumerate(matches):
    fid = m.group(1)
    title = m.group(2).strip()
    start_pos = m.start()
    end_pos = matches[idx + 1].start() if idx + 1 < len(matches) else len(all_detail_text)
    body = all_detail_text[start_pos:end_pos].strip()
    
    parsed_findings[fid] = {
        'id': fid,
        'title': title,
        'full_text': body
    }

print(f"Total parsed findings: {len(parsed_findings)}")
missing = set(all_40_cleaned) - set(parsed_findings.keys())
print("Missing findings:", missing)

with open('all_40_findings.json', 'w', encoding='utf-8') as f:
    json.dump(parsed_findings, f, indent=2, ensure_ascii=False)

with open('all_40_findings.md', 'w', encoding='utf-8') as f:
    for fid in all_40_cleaned:
        if fid in parsed_findings:
            item = parsed_findings[fid]
            f.write(f"# {item['id']} · {item['title']}\n\n")
            f.write(item['full_text'] + "\n\n" + "="*80 + "\n\n")

print("Generated all_40_findings.json and all_40_findings.md successfully!")
