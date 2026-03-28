import os
import json

root_dirs = [
    r"d:\C# and Python App\ProgrammingTutor\Content\lessons\python",
    r"d:\C# and Python App\ProgrammingTutor\Content\lessons\csharp"
]

missing_data = []

for root_dir in root_dirs:
    if not os.path.exists(root_dir):
        continue
    for filename in os.listdir(root_dir):
        if filename.endswith(".json"):
            filepath = os.path.join(root_dir, filename)
            try:
                with open(filepath, 'r', encoding='utf-8') as f:
                    data = json.load(f)
                
                challenges = data.get("Challenges", data.get("challenges", []))
                for i, chal in enumerate(challenges):
                    # Check for missing ExpectedOutput (case insensitive)
                    has_eo = any(k.lower() == "expectedoutput" for k in chal.keys())
                    if not has_eo:
                        missing_data.append(f"{filename} - Challenge {i+1}: Missing ExpectedOutput")
                    
                    # Check for empty RequiredKeywords
                    keywords = chal.get("RequiredKeywords", chal.get("requiredKeywords", []))
                    if not keywords:
                        missing_data.append(f"{filename} - Challenge {i+1}: Missing RequiredKeywords")
                        
            except Exception as e:
                missing_data.append(f"{filename}: Error reading - {str(e)}")

if missing_data:
    print("\n".join(missing_data))
else:
    print("All good!")
