using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MonRendering : ActorRendering
{
	[SerializeField] private List<SpriteRenderer> childsRnd;
	private CancellationTokenSource invincibilityCancellation;
	private CancellationTokenSource damageFlashCancellation;
	private CancellationTokenSource rageCancellation;
	private CancellationTokenSource transformCancellation;
	private CancellationTokenSource appearCancellation;
	private SortedDictionary<eMonRenderingEffect, Action> renderingEffects;

	public void Initialize()
	{
		renderingEffects = new SortedDictionary<eMonRenderingEffect, Action>();
	}
	
	public void AddChild(SpriteRenderer newChild)
	{
		childsRnd.Add(newChild);
		newChild.material.CopyPropertiesFromMaterial(compRnd.material);
		newChild.color = compRnd.color;
	}

	public void RemoveChild(SpriteRenderer newChild)
	{
		newChild.material.DisallowPixelColorChange();
		newChild.material.DisallowHitEffect();
		newChild.color = WorldColors.RealWhite;
		
		childsRnd.Remove(newChild);
	}

	private void AddEffect(eMonRenderingEffect newEffect, Action newAction)
	{
		renderingEffects[newEffect] = newAction;
		PlayFirstPriorityEffect();
	}

	private void RemoveEffect(eMonRenderingEffect effect)
	{
		if (!renderingEffects.ContainsKey(effect)) return;
		
		renderingEffects.Remove(effect);
		
		compRnd.color = WorldColors.RealWhite;
		compRnd.material.DisallowPixelColorChange();
		compRnd.material.DisallowHitEffect();

		ChildrenCopyCompRnd();

		PlayFirstPriorityEffect();
	}

	private void ChildrenCopyCompRnd()
	{
		foreach (SpriteRenderer childRnd in childsRnd)
		{
			childRnd.material.CopyPropertiesFromMaterial(compRnd.material);
			childRnd.color = compRnd.color;
		}
	}

	private void PlayFirstPriorityEffect()
	{
		compRnd.color = WorldColors.RealWhite;
		compRnd.material.DisallowPixelColorChange();
		compRnd.material.DisallowHitEffect();

		ChildrenCopyCompRnd();
		
		foreach (var action in renderingEffects)
		{
			action.Value?.Invoke();
			break;
		}
	}

	//### GENE PALETTE #################################################################################################

	public void PlayGenePalette(Mon target, MonData source)
	{
		AddEffect(eMonRenderingEffect.GenePalette, () => ShowGenePalette(target, source));	
	}
	
	private void ShowGenePalette(Mon target, MonData source)
	{
		for (int i = 0; i < target.colors.Length; i++)
		{
			compRnd.material.ChangePixelColor(
				target.colors[i],
				source.colors[i],
				i);
		}

		compRnd.material.AllowPixelColorChange(i: 3);

		ChildrenCopyCompRnd();
	}

	public void HideGenePalette()
	{
		RemoveEffect(eMonRenderingEffect.GenePalette);
	}
	
	//### INVINCIBILITY PALETTE ########################################################################################

	public void PlayInvincibilityPalette()
	{
		PlayInvincibilityPaletteTask().Forget();
	}
	
	private async UniTask PlayInvincibilityPaletteTask()
	{
		invincibilityCancellation ??= new CancellationTokenSource();

		while (true)
		{
			await UniTask.Delay(TimeSpan.FromSeconds(WorldValues.DAMAGE_FLASH_DURATION), 
				cancellationToken: invincibilityCancellation.Token);
			
			AddEffect(eMonRenderingEffect.InvincibilityPalette, HideSprite);
		
			await UniTask.Delay(TimeSpan.FromSeconds(WorldValues.DAMAGE_FLASH_DURATION), 
				cancellationToken: invincibilityCancellation.Token);
			
			RemoveEffect(eMonRenderingEffect.InvincibilityPalette);
		}
	}
	
	private void HideSprite()
	{
		compRnd.color = WorldColors.Transparent;
		ChildrenCopyCompRnd();
	}

	public void HideInvincibilityPalette()
	{
		Helper.FreeCancellationToken(ref invincibilityCancellation);
		RemoveEffect(eMonRenderingEffect.InvincibilityPalette);
	}
	
	//### DAMAGE FLASHING ##############################################################################################
	
	public void PlayDamageFlashing(int damageReceived)
	{
		if (damageReceived <= 0) return; // Fake Damage like Ball Throw
		
		PlayDamageFlashingTask().Forget();
	}
	
	private async UniTask PlayDamageFlashingTask()
	{
		damageFlashCancellation ??= new CancellationTokenSource();
		
		AddEffect(eMonRenderingEffect.DamageFlash, DamageFlashingHitWhite);
		
		await UniTask.Delay(TimeSpan.FromSeconds(WorldValues.DAMAGE_FLASH_DURATION), 
			cancellationToken: damageFlashCancellation.Token);
		
		RemoveEffect(eMonRenderingEffect.DamageFlash);
	}

	private void DamageFlashingHitWhite()
	{
		compRnd.material.ChangeHitEffectColor(WorldColors.RealWhite);
		compRnd.material.AllowHitEffect();
		
		ChildrenCopyCompRnd();
	}
	
	//### RAGE FLASHING ################################################################################################

	public async UniTask PlayRageFlashing()
	{
		rageCancellation ??= new CancellationTokenSource();

		while (rageCancellation != null)
		{
			AddEffect(eMonRenderingEffect.RagePulse, RageFlashingRed);
			
			await UniTask.Delay(TimeSpan.FromSeconds(WorldValues.RAGE_FLASH_DURATION), 
				cancellationToken: rageCancellation.Token);
			
			RemoveEffect(eMonRenderingEffect.RagePulse);
			
			await UniTask.Delay(TimeSpan.FromSeconds(WorldValues.RAGE_FLASH_DURATION), 
				cancellationToken: rageCancellation.Token);
		}
	}
	
	private void RageFlashingRed()
	{
		compRnd.material.ChangePixelColor(WorldColors.RealWhite, WorldColors.White, 0);
		compRnd.material.ChangePixelColor(WorldColors.RealBlack, WorldColors.RedRage, 1);
		compRnd.material.AllowPixelColorChange(0f, 2);
		
		ChildrenCopyCompRnd();
	}

	public void StopRageMode()
	{
		Helper.FreeCancellationToken(ref rageCancellation);
		RemoveEffect(eMonRenderingEffect.RagePulse);
	}
	
	//### TRANSFORM FLASHING ###########################################################################################
	
	public void PlayTransformPreFlashing(Mon target)
	{
		PlayTransformPreFlashingTask(target).Forget();
	}
	
	private async UniTask PlayTransformPreFlashingTask(Mon target)
	{
		transformCancellation ??= new CancellationTokenSource();
		
		AddEffect(eMonRenderingEffect.TransformFlash, () => ShowTransformPalette(target));
		await CustomDelay(0.04f, transformCancellation.Token);
		
		AddEffect(eMonRenderingEffect.TransformFlash, TransformFlashingWhite);
		await CustomDelay(0.05f, transformCancellation.Token);
	}
	
	public void PlayTransformPostFlashing(Mon target)
	{
		PlayTransformPostFlashingTask(target).Forget();
	}
	
	private async UniTask PlayTransformPostFlashingTask(Mon target)
	{
		transformCancellation ??= new CancellationTokenSource();
		
		AddEffect(eMonRenderingEffect.TransformFlash, TransformFlashingWhite);
		await CustomDelay(0.04f, transformCancellation.Token);
		
		AddEffect(eMonRenderingEffect.TransformFlash, () => ShowTransformPalette(target));
		await CustomDelay(0.04f, transformCancellation.Token);
		
		RemoveEffect(eMonRenderingEffect.TransformFlash);
	}
	
	private void ShowTransformPalette(Mon target)
	{
		for (int i = 0; i < target.colors.Length; i++)
		{
			compRnd.material.ChangePixelColor(
				target.colors[i],
				WorldColors.White,
				i);
		}
		compRnd.material.ChangePixelColor(
			WorldColors.Black, 
			GameController.Instance.MonDataCollection.collection[eMonId.Gene].colors[1], 
			3);

		compRnd.material.AllowPixelColorChange(0.5f);
		
		ChildrenCopyCompRnd();
	}
	
	private void TransformFlashingWhite()
	{
		compRnd.material.ChangeHitEffectColor(WorldColors.White);
		compRnd.material.AllowHitEffect();
		
		ChildrenCopyCompRnd();
	}
	
	//### APPEAR FLASHING ##############################################################################################
	
	public void PlayAppearFlashing(Mon target)
	{
		PlayAppearFlashingTask(target).Forget();
	}
	
	private async UniTask PlayAppearFlashingTask(Mon target)
	{
		appearCancellation ??= new CancellationTokenSource();
		
		AddEffect(eMonRenderingEffect.AppearFlash, () => ShowAppearPalette(target));
		await CustomDelay(0.1f, appearCancellation.Token);
		
		AddEffect(eMonRenderingEffect.AppearFlash, TransformFlashingWhite);
		await CustomDelay(0.1f, appearCancellation.Token);
		
		RemoveEffect(eMonRenderingEffect.AppearFlash);
	}
	
	private void ShowAppearPalette(Mon target)
	{
		for (int i = 0; i < target.colors.Length; i++)
		{
			compRnd.material.ChangePixelColor(
				target.colors[i],
				WorldColors.White,
				i);
		}
		compRnd.material.ChangePixelColor(
			WorldColors.Black, 
			target.colors[1], 
			3);

		compRnd.material.AllowPixelColorChange(0.5f);
		
		ChildrenCopyCompRnd();
	}
	
	//##################################################################################################################
	
	private async UniTask CustomDelay(float seconds, CancellationToken token)
	{
		float startTime = Time.unscaledTime;
		while (Time.unscaledTime - startTime < seconds)
		{
			await UniTask.Yield(PlayerLoopTiming.Update, token);
		}
	}

	private void OnDestroy()
	{
		Helper.FreeCancellationToken(ref damageFlashCancellation);
		Helper.FreeCancellationToken(ref rageCancellation);
		Helper.FreeCancellationToken(ref invincibilityCancellation);
		Helper.FreeCancellationToken(ref transformCancellation);
	}
}