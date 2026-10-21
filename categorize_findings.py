import json

with open('all_40_findings.json', 'r', encoding='utf-8') as f:
    findings = json.load(f)

print(f"Total findings: {len(findings)}")

by_area = {}
by_sev = {}

for fid, data in findings.items():
    text = data['full_text']
    # Extract Severity, Area / type, Page, Steps, Expected, Actual, Suggested fix
    sev = "Unknown"
    area = "Unknown"
    for line in text.split('\n'):
        if line.startswith("Severity"):
            idx = text.split('\n').index(line)
            sev = text.split('\n')[idx+1]
        elif line.startswith("Area / type"):
            idx = text.split('\n').index(line)
            area = text.split('\n')[idx+1]
            
    data['severity_clean'] = sev
    data['area_clean'] = area
    
    by_area.setdefault(area, []).append(fid)
    by_sev.setdefault(sev, []).append(fid)

print("\n--- BY SEVERITY ---")
for s, ids in by_sev.items():
    print(f"{s} ({len(ids)}): {', '.join(ids)}")

print("\n--- BY AREA ---")
for a, ids in by_area.items():
    print(f"{a} ({len(ids)}): {', '.join(ids)}")

print("\n--- DETAILED SUMMARY LIST ---")
for fid, data in sorted(findings.items()):
    print(f"[{fid}] ({data['severity_clean']}) {data['title']}")
