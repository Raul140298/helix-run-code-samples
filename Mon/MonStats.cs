using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MonStats : MonoBehaviour
{
	private MonModel compModel;

	[SerializeField] private int health;
	[SerializeField] private int maxHealth;

	[SerializeField] private int energy;
	[SerializeField] private int maxEnergy;
	
	private CancellationTokenSource recoveryTransformEnergyCancellation;

	[SerializeField] private float speed;
	[SerializeField] private float forceDamping;
	private float speedTerrainModifier;

	public event Action OnDeath;
	public event Action OnReceiveDamage;
	public event Action<int> OnReceiveDamageInt;
	public event Action<MonModel> OnReceiveDamageMonModel;
	public event Action OnStatsChange;
	public event Action OnEnergyRanOut;

	//### NEW MON ######################################################################################################

	public void Initialize()
	{
		maxHealth = 0;
		maxEnergy = 0;

		AddHealth(compModel.MonInstance);
		AddEnergy(compModel.MonInstance);
		
		MonInstanceWasChanged();
	}

	//### HEALTH #######################################################################################################

	public void AddHealth(Mon newInstance)
	{
		newInstance.OnBeforeEvolve -= RemoveHealthInherited;
		newInstance.OnAfterEvolve -= AddHealthInherited;
		newInstance.OnBeforeEvolve += RemoveHealthInherited;
		newInstance.OnAfterEvolve += AddHealthInherited;

		ChangeMaxHealth(newInstance.health);
	}

	public void AddHealthInherited(Mon newInstance)
	{
		newInstance.OnBeforeEvolve -= RemoveHealthInherited;
		newInstance.OnAfterEvolve -= AddHealthInherited;
		newInstance.OnBeforeEvolve += RemoveHealthInherited;
		newInstance.OnAfterEvolve += AddHealthInherited;

		ChangeMaxHealth(newInstance.healthInherited);
	}

	public void RemoveHealth(Mon instance)
	{
		ChangeMaxHealth(-instance.health);
	}

	private void RemoveHealthInherited(Mon instance)
	{
		ChangeMaxHealth(-instance.healthInherited);
	}

	public void ReceiveDamage(MonModel source, int dmg, eMonType dmgType)
	{
		if (source) OnReceiveDamageMonModel?.Invoke(source);
		OnReceiveDamage?.Invoke();
		OnReceiveDamageInt?.Invoke(dmg);

		if (dmg <= 0) return;

		if (compModel.IsAPlayer)
		{
			Feedback.Do(eFeedbackType.Sfx_Hit1);
			GameController.Instance.CameraController.ShakeCamera(4.0f, 50f, 0.2f);
		}
		else
		{
			Feedback.Do(eFeedbackType.Sfx_Hit2);
			compModel.CompRendering.Shake();
		}

		health = Math.Max(0, health - dmg);

		if (health == 0)
		{
			OnDeath?.Invoke();
		}

		StatsChange();
	}

	public void ReceiveHeal(int heal)
	{
		if (heal <= 0) return;

		health = Math.Clamp(health + heal, 0, maxHealth);

		StatsChange();
	}

	public void ChangeMaxHealth(int newHealth)
	{
		if (maxHealth == 0) health = newHealth;
		maxHealth += newHealth;
		if (newHealth > 0) health += newHealth;
		health = Mathf.Min(health, maxHealth);
		StatsChange();
	}

	public bool CanConsumeHealth(int healthConsumption)
	{
		return health - healthConsumption > 0;
	}
	
	public void ConsumeHealth(int healthConsumed)
	{
		health -= healthConsumed;

		StatsChange();
	}

	//### ENERGY #######################################################################################################

	public void AddEnergy(Mon newInstance)
	{
		newInstance.OnBeforeEvolve -= RemoveEnergyInherited;
		newInstance.OnAfterEvolve -= AddEnergyInherited;
		newInstance.OnBeforeEvolve += RemoveEnergyInherited;
		newInstance.OnAfterEvolve += AddEnergyInherited;

		ChangeMaxEnergy(newInstance.energy);
	}

	public void AddEnergyInherited(Mon newInstance)
	{
		newInstance.OnBeforeEvolve -= RemoveEnergyInherited;
		newInstance.OnAfterEvolve -= AddEnergyInherited;
		newInstance.OnBeforeEvolve += RemoveEnergyInherited;
		newInstance.OnAfterEvolve += AddEnergyInherited;

		ChangeMaxEnergy(newInstance.energyInherited);
	}

	public void RemoveEnergy(Mon instance)
	{
		ChangeMaxEnergy(-instance.energy);
	}

	private void RemoveEnergyInherited(Mon instance)
	{
		ChangeMaxEnergy(-instance.energyInherited);
	}

	public void ChangeMaxEnergy(int newEnergy)
	{
		if (maxEnergy == 0) energy = newEnergy;
		maxEnergy += newEnergy;
		if (newEnergy > 0) energy += newEnergy;
		energy = Mathf.Min(energy, maxEnergy);
		StatsChange();
	}
	
	public void ReceiveEnergy(int newEnergy = 1)
	{
		energy = Math.Clamp(energy + newEnergy, 0, maxEnergy);

		StatsChange();
	}

	public bool CanConsumeEnergy(int energyConsumption)
	{
		return energy - energyConsumption >= 0;
	}
	
	public void ConsumeEnergy(int energyConsumed)
	{
		energy = Math.Clamp(energy - energyConsumed, 0, maxEnergy);

		StatsChange();

		RecoveryTransformationEnergy().Forget();
	}

	private async UniTaskVoid RecoveryTransformationEnergy()
	{
		if (recoveryTransformEnergyCancellation != null) return;

		recoveryTransformEnergyCancellation ??= new CancellationTokenSource();

		while (energy < maxEnergy)
		{
			await UniTask.WaitUntil(()=> compModel.CompAbilities.IsUsingAbility == false, 
				cancellationToken: recoveryTransformEnergyCancellation.Token);
			
			if (energy == 0) OnEnergyRanOut?.Invoke();
			
			energy++;
			
			StatsChange();
			
			await UniTask.Delay(TimeSpan.FromSeconds(1f),
				cancellationToken: recoveryTransformEnergyCancellation.Token);
		}

		Helper.FreeCancellationToken(ref recoveryTransformEnergyCancellation);
	}

	//### SPEED ########################################################################################################

	public void GetMovementConstantModifier(eTerrain terrain)
	{
		float m = 0;

		if (compModel.MonInstance.HasMovement(eMonMovementType.Air))
		{
			speedTerrainModifier = m;
			return;
		}

		if (compModel.MonInstance.HasMovement(eMonMovementType.Water))
		{
			switch (terrain)
			{
				case eTerrain.Water:
					m += WorldValues.WATER_IN_WATER_MODIFIER;
					break;
				case eTerrain.Ground:
					m += WorldValues.WATER_IN_GROUND_MODIFIER;
					break;
			}
		}

		if (compModel.MonInstance.HasMovement(eMonMovementType.Ground))
		{
			switch (terrain)
			{
				case eTerrain.Ground:
					m += WorldValues.GROUND_IN_GROUND_MODIFIER;
					break;
				case eTerrain.Water:
					m += WorldValues.GROUND_IN_WATER_MODIFIER;
					break;
			}
		}

		speedTerrainModifier = m;
	}

	//##################################################################################################################

	public void StatsChange()
	{
		OnStatsChange?.Invoke();
	}

	public void MonInstanceWasChanged()
	{
		speed = compModel.MonInstance.isAPlayerMon ? compModel.MonInstance.speedPlayer : compModel.MonInstance.speed;
		forceDamping = compModel.MonInstance.forceDamping;
	}

	//### SETTERS N GETTERS ############################################################################################

	public float Speed
	{
		get
		{
			float speedModifier = compModel.CompPassives.GetModifier(eMonPassiveTarget.Speed);
			return speed + speedTerrainModifier + speedModifier;
		}
	}

	public float ForceDamping
	{
		get
		{
			float forceDampingModifier = compModel.CompPassives.GetModifier(eMonPassiveTarget.Weight);
			return forceDamping + forceDampingModifier;
		}
	}

	public int Health => health;
	public int MaxHealth => maxHealth;
	public int Energy => energy;
	public int MaxEnergy => maxEnergy;
	
	public MonModel CompModel
	{
		set => compModel = value;
	}
	
	//##################################################################################################################

	private void OnDestroy()
	{
		Helper.FreeCancellationToken(ref recoveryTransformEnergyCancellation);
		
		OnDeath = null;
		OnReceiveDamage = null;
		OnReceiveDamageMonModel = null;
		OnReceiveDamageInt = null;
		OnStatsChange = null;
	}
}