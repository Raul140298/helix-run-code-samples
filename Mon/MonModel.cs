using System;
using System.Threading;
using Sirenix.OdinInspector;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class MonModel : SerializedMonoBehaviour
{
	[Header("Comps")] 
	[SerializeField] private Rigidbody2D compRb;
	[SerializeField] private MonRendering compRendering;
	[SerializeField] private MonAbilities compAbilities;
	[SerializeField] private MonCollisions compCollisions;
	[SerializeField] private MonTerrain compTerrain;
	[SerializeField] private MonStats compStats;
	[SerializeField] private MonPassives compPassives;
	[SerializeField] private MonMovement compMovement;
	[SerializeField] private Collider2D compCol;
	
	private ShadowModel compShadow;

	[Space(15)] 
	[Header("Data")] 
	[SerializeField] private Mon monInstance;
	
	[Space(15)] 
	[Header("State")] 
	[EnumToggleButtons] [SerializeField] [ReadOnly] protected eMonState state;

	private bool isInCamera;
	private bool isAPlayer;
	
	private eCollisionTag collisionTag;
	
	public event Action OnDeathStart;
	public event Action<bool> OnBeingCaptured;
	public event Action OnDeathEnd;
	public event Action<MonModel, bool> OnDeathEndMonModelBool;
	public event Action<MonModel> OnDeathEndMonModel;
	public event Action OnMonDataChange;
	public event Action<MonModel> OnMonCollision;
	public event Action<ConsumableModel> OnConsumableCollision;
	
	private CancellationTokenSource invincibilityCancellation;

	//##################################################################################################################

	public void AwakeMon()
	{
		compAbilities.CompModel = this;
		compAbilities.CompRendering = compRendering;
		compCollisions.CompModel = this;
		compPassives.CompModel = this;
		compStats.CompModel = this;
		compMovement.CompModel = this;
	}

	public void Initialize(Mon monInstance)
	{
		this.monInstance = monInstance;
		
		compStats.Initialize();
		compStats.OnReceiveDamageInt += compRendering.PlayDamageFlashing;
		compStats.OnDeath += DeathStart;
		compTerrain.OnTerrainChange += compStats.GetMovementConstantModifier;
		
		compPassives.SetInitialPassives(null, monInstance);
		monInstance.OnPassiveAdded += compPassives.AddNewPassive;
		
		compAbilities.Initialize();
		
		compRendering.Initialize();
	}

	public void SetShadow(ShadowModel shadow)
	{
		if (compShadow != null && compShadow.IsActive) return;
		
		compShadow = shadow;
		compShadow.SetData(this.gameObject, eShadowSize.Mon);
		compShadow.Activate();

		OnDeathEnd += compShadow.Hide;
	}
	
	//### INVINCIBILITY ################################################################################################

	public void StartDamageInvincibility()
	{
		if (invincibilityCancellation != null) return;

		StartInvincibility(WorldValues.INVINCIBILITY_DURATION, true).Forget();
	}

	public void StartAbilityDashInvincibility()
	{
		if (invincibilityCancellation != null) return;
		
		StartInvincibility(0.1f, false).Forget();
	}
	
	private async UniTaskVoid StartInvincibility(float duration, bool withPalette)
	{
		invincibilityCancellation ??= new CancellationTokenSource();
		
		if (withPalette) compRendering.PlayInvincibilityPalette();

		await UniTask.Delay(TimeSpan.FromSeconds(duration), 
			cancellationToken: invincibilityCancellation.Token);
		
		if (withPalette) compRendering.HideInvincibilityPalette();
		
		Helper.FreeCancellationToken(ref invincibilityCancellation);
	}

	//### TRIGGERS #####################################################################################################

	private void DeathStart()
	{
		OnDeathStart?.Invoke();
	}

	public void BeingCaptured(bool wasCaptured)
	{
		OnBeingCaptured?.Invoke(wasCaptured);
	}
	
	public void DeathEnd(bool wasCaptured)
	{
		OnDeathEnd?.Invoke();
		OnDeathEndMonModelBool?.Invoke(this, wasCaptured);
		OnDeathEndMonModel?.Invoke(this);
	}
	
	public void MonDataChange()
	{
		OnMonDataChange?.Invoke();
	}

	public void MonCollision(MonModel target)
	{
		OnMonCollision?.Invoke(target);
	}

	public void ConsumableCollision(ConsumableModel target)
	{
		OnConsumableCollision?.Invoke(target);
	}

	//### SETTERS N GETTERS ############################################################################################
	
	public eMonState State
	{
		get => state;
		set => state = value;
	}

	public eCollisionTag CollisionTag
	{
		get => collisionTag;
		set => collisionTag = value;
	}

	public Mon MonInstance
	{
		get => monInstance;
		set => monInstance = value;
	}
	
	public bool IsInCamera
	{
		get => isInCamera;
		set => isInCamera = value;
	}
	
	public bool IsAPlayer
	{
		get => isAPlayer;
		set => isAPlayer = value;
	}
	
	public MonCollisions CompCollisions => compCollisions;
	public MonRendering CompRendering => compRendering;
	public MonAbilities CompAbilities => compAbilities;
	public MonTerrain CompTerrain => compTerrain;
	public MonStats CompStats => compStats;
	public MonPassives CompPassives => compPassives;
	public MonMovement CompMovement => compMovement;
	public Collider2D CompCol => compCol;
	public Rigidbody2D CompRb => compRb;
	
	public bool IsInvincible => invincibilityCancellation != null;
	
	//##################################################################################################################

	private void OnDestroy()
	{
		OnDeathStart = null;
		OnBeingCaptured = null;
		OnDeathEnd = null;
		OnDeathEndMonModelBool = null;
		OnDeathEndMonModel = null;
		OnMonDataChange = null;
		OnMonCollision = null;
		OnConsumableCollision = null;
		
		Helper.FreeCancellationToken(ref invincibilityCancellation);
	}
}