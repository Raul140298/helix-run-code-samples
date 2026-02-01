using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Mon
{
	public eMonId id;
	public eMonId family;
	public eMonType type;
	public eMonMovementType movementType;
	public eMonWeight weight;
	public float forceDamping;
	public Ability[] abilities;
	public float engagedVisionRadius;
	public float followVisionRadius;
	public Color[] colors;
	public List<eMonPassive> passives;
	public int tier;
	public int health;
	public int healthInherited;
	public int energy;
	public int energyInherited;
	public int speed;
	public int speedPlayer;
	public int currentExpForEvo;
	public int neededExpForEvo;
	public MonAnimationData animData;
	public Sprite icon;
	public bool isAPlayerMon;
	public bool startWith2Abilities;
	public event Action OnExpForEvoAdded;
	public event Action<Mon> OnBeforeEvolve;
	public event Action<Mon> OnAfterEvolve;
	public event Action<Mon, PassiveData> OnPassiveAdded; //<----------------------------NEVER USED--------------------!

	public MonDna monDna;

	public Mon(eMonId familyId, int tier, bool isAPlayerMon, bool startWith2Abilities, MonDna monDna = null)
	{
		this.startWith2Abilities = startWith2Abilities;
		this.isAPlayerMon = isAPlayerMon;
		
		if (monDna == null)
		{
			CreateMonLine(familyId);
		}
		else
		{
			this.monDna = monDna;
		}
		
		SetData(this.monDna.lines[tier]);
	}

	private void CreateMonLine(eMonId familyId)
	{
		monDna = new MonDna();
		
		var collection = GameController.Instance.MonDataCollection.collection;
		MonData newData = collection[familyId];
		
		PassiveDataCollection passiveDataCollection = GameController.Instance.PassiveDataCollection;
		List<eMonPassive> auxPassives = new();
		eMonPassive newPassive = new();
		
		while (newData != null)
		{
			eMonType passiveType = GetRandomType(newData.type);

			if (passiveDataCollection.collection.ContainsKey(passiveType))
			{
				List<eMonPassive> passivesAvailable = 
					new List<eMonPassive>(passiveDataCollection.collection[passiveType].Keys);
				
				passivesAvailable.RemoveAll(
					passive => !passiveDataCollection.collection[passiveType][passive].active);

				if (auxPassives.Count > 0) passivesAvailable.Remove(newPassive);
				
				if (passivesAvailable.Count > 0)
				{
					newPassive = passivesAvailable[UnityEngine.Random.Range(0, passivesAvailable.Count)];
			
					auxPassives.Add(newPassive);
					passivesAvailable.Remove(newPassive);	
				}
			}
			
			monDna.lines[newData.tier] = new MonLine(newData, auxPassives, startWith2Abilities);
			if (monDna.startIndex == -1) monDna.startIndex = newData.tier;
			monDna.lastIndex = newData.tier;
			newData = newData.evolution;
		}
	}

	private void SetData(MonLine monLine)
	{
		MonData data = GameController.Instance.MonDataCollection.collection[monLine.id];
		
		id = data.id;
		family = data.family;
		type = data.type;
		tier = data.tier;
		health = data.health;
		healthInherited = data.healthInherited;
		energy = data.energy;
		energyInherited = data.energyInherited;
		movementType = data.movementType;
		speed = data.speed;
		speedPlayer = data.speedPlayer;
		weight = data.weight;
		forceDamping = data.forceDamping;

		if (abilities != null)
		{
			foreach (var ability in abilities)
			{
				ability.FreeCooldownStateChange();
			}
		}

		abilities = new Ability[monLine.abilities.Count];
		for (int i = 0; i < abilities.Length; i++)
		{
			Dictionary<eMonMovementType, AbilityData> ability =
				GameController.Instance.AbilityDataCollection.GetAllAbilitiesTerrain(monLine.abilities[i]);
			
			abilities[i] = ability != null ? new Ability(monLine.abilities[i], ability) : null;
		}

		passives = new(monLine.passives);

		engagedVisionRadius = data.engagedVisionRadius;
		followVisionRadius = data.followVisionRadius;
		
		colors = data.colors;

		animData = data.animData;
		icon = data.icon;

		currentExpForEvo = 0;
		neededExpForEvo = (data.tier + 1) * 5;
	}

	public void Evolve()
	{
		OnBeforeEvolve?.Invoke(this);

		SetData(monDna.lines[tier + 1]);
		
		OnAfterEvolve?.Invoke(this);
	}

	public void AddExpForEvo()
	{
		if (!CanEvolve()) return;
		
		currentExpForEvo += 1;
		OnExpForEvoAdded?.Invoke();
	}
	
	public bool HasMovement(eMonMovementType type)
	{
		return (movementType & type) != 0;
	}
	
	public bool HasType(eMonType type)
	{
		return (this.type & type) != 0;
	}

	public eMonType GetRandomType(eMonType type)
	{
		var activeTypes = Enum.GetValues(typeof(eMonType))
			.Cast<eMonType>()
			.Where(t => t != 0 && type.HasFlag(t))
			.ToArray();

		return activeTypes[UnityEngine.Random.Range(0, activeTypes.Length)];
	}

	public bool HasEvolution()
	{
		if (tier + 1 >= monDna.lines.Length) return false;
		return monDna.lines[tier + 1] != null;
	}

	public bool CanEvolve()
	{
		return HasEvolution() && currentExpForEvo >= neededExpForEvo;
	}

	public void OnDestroy()
	{
		OnExpForEvoAdded = null;
		OnBeforeEvolve = null;
		OnAfterEvolve = null;
		OnPassiveAdded = null;

		foreach (var ability in abilities)
		{
			ability.OnDestroy();
		}
	}
}