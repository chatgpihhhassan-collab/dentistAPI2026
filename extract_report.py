import sys
import re
import json

with open('report_dentia_full.txt', 'r', encoding='utf-8') as f:
    text = f.read()

pages = text.split('==================== PAGE ')

# First: let's extract the findings table from pages 8-11
print("=== EXTRACTING SUMMARY TABLE ===")
findings_table = []
for p_idx in range(8, 12):
    p_text = pages[p_idx]
    # Look for table entries
    # The summary table lists ID, Severity, Area, Type, Title
    lines = [l.strip() for l in p_text.split('\n') if l.strip()]
    i = 0
    while i < len(lines):
        m = re.match(r'^([A-Z]{2,4}-\s*\d{3})$', lines[i])
        if m:
            fid = re.sub(r'\s+', '', m.group(1))
            sev = lines[i+1] if i+1 < len(lines) else ''
            area = lines[i+2] if i+2 < len(lines) else ''
            typ = lines[i+3] if i+3 < len(lines) else ''
            title = lines[i+4] if i+4 < len(lines) else ''
            # sometimes title wraps
            j = i + 5
            extra_title = []
            while j < len(lines) and not re.match(r'^[A-Z]{2,4}-\s*\d{3}$', lines[j]) and lines[j] not in ['Findings', 'Summary', 'Critical', 'High', 'Medium', 'Low', 'ID', 'Severity', 'Area', 'Type', 'Title'] and not re.match(r'^\d+\s*====================', lines[j]):
                # If next lines are not start of new finding, could be wrapped title
                # but be careful
                if j < len(lines) - 4 and re.match(r'^[A-Z]{2,4}-\s*\d{3}$', lines[j+1]):
                    # lines[j] is probably the rest of title
                    extra_title.append(lines[j])
                    j += 1
                    break
                elif any(lines[j].startswith(k) for k in ['APT-', 'AI-', 'CFG-', 'NAV-', 'PAT-', 'PERF-', 'REG-', 'SEC-', 'UX-', 'WEB-']):
                    break
                else:
                    extra_title.append(lines[j])
                    j += 1
            if extra_title:
                title = title + ' ' + ' '.join(extra_title)
            findings_table.append({
                'id': fid,
                'severity': sev,
                'area': area,
                'type': typ,
                'title': title
            })
            i = j
        else:
            i += 1

print(f"Total findings in summary table: {len(findings_table)}")
for item in findings_table:
    print(f"{item['id']} | {item['severity']} | {item['area']} | {item['title'][:60]}")

# Second: extract detailed pages
detailed_findings = {}
for p_idx in range(11, len(pages)):
    p_text = pages[p_idx]
    # Search for finding headers
    # Lines on detail pages typically have:
    # APT-001 · 'Save Changes' does nothing...
    # or finding ID near top
    m = re.search(r'\b([A-Z]{2,4}-\d{3})\b', p_text[:200])
    if m:
        fid = m.group(1)
        if fid not in detailed_findings:
            detailed_findings[fid] = {
                'id': fid,
                'page': p_idx,
                'text': p_text.strip()
            }
        else:
            # continuation page!
            detailed_findings[fid]['text'] += "\n--- CONTINUED (Page " + str(p_idx) + ") ---\n" + p_text.strip()

print(f"\nTotal detailed findings found: {len(detailed_findings)}")
# Check which findings in summary table are missing detail
summary_ids = [f['id'] for f in findings_table]
detail_ids = list(detailed_findings.keys())
print("Summary IDs count:", len(summary_ids))
print("Detail IDs count:", len(detail_ids))
missing = set(summary_ids) - set(detail_ids)
print("Missing in detail:", missing)

# Save combined into full_report_parsed.json
with open('full_report_parsed.json', 'w', encoding='utf-8') as f:
    json.dump({
        'table': findings_table,
        'details': detailed_findings
    }, f, indent=2, ensure_ascii=False)
