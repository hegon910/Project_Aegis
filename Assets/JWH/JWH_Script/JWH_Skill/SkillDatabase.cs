using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillDatabase", menuName = "Skills/Skill Database")]
public class SkillDatabase : ScriptableObject
{
    public List<SkillData> allSkills;

    private Dictionary<string, SkillData> skillDictionary;

    public void BuildDictionary()
    {
        skillDictionary = new Dictionary<string, SkillData>();
        foreach (var skill in allSkills)
        {
            if (skill != null && !skillDictionary.ContainsKey(skill.skillID))
            {
                skillDictionary.Add(skill.skillID, skill);
            }
        }
    }

    public SkillData GetSkillByID(string id)
    {
        if (skillDictionary == null)
        {
            BuildDictionary();
        }

        if (string.IsNullOrEmpty(id)) return null;

        skillDictionary.TryGetValue(id, out SkillData skill);
        return skill;
    }
}

