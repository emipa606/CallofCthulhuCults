using System;
using System.Net.Mime;
using CultOfCthulhu;
using UnityEngine;
using Verse;

namespace CallOfCthulhu
{
	public class Dialog_CosmicEntityInfoBox : Window
	{
		private const float InitialWidth = 640f;
		private const float InitialHeight = 800f;
		public string text;

		public string title;

		public string buttonAText;

		public Texture2D image;

		public Action buttonAAction;

		public bool buttonADestructive;

		public string buttonBText;

		public Action buttonBAction;

		public string buttonCText;

		public Action buttonCAction;

		public bool buttonCClose = true;

		public float interactionDelay = 0f;

		public Action acceptAction;

		public Action cancelAction;

		private Vector2 scrollPosition = Vector2.zero;

		private readonly float creationRealTime = -1f;

		public Dialog_CosmicEntityInfoBox(CosmicEntity entity)
		{
			text = entity.Info();
			title = entity.LabelCap;
			if (buttonAText.NullOrEmpty())
			{
				buttonAText = "OK".Translate();
			}
			if (entity.Def.Portrait != "")
				image = ContentFinder<Texture2D>.Get(entity.Def.Portrait);
			forcePause = true;
			absorbInputAroundWindow = true;
			creationRealTime = RealTime.LastRealTime;
			onlyOneOfTypeAllowed = false;
			closeOnAccept = true;
			closeOnCancel = true;
		}

		public override Vector2 InitialSize
		{
			get
			{
				return new Vector2(InitialWidth, InitialHeight);
			}
		}

		private float get_TimeUntilInteractive()
		{
			return interactionDelay - (Time.realtimeSinceStartup - creationRealTime);
		}

		private bool get_InteractionDelayExpired()
		{
			return get_TimeUntilInteractive() <= 0f;
		}

		public override void DoWindowContents(Rect inRect)
		{
			float num = inRect.y;
			if (!title.NullOrEmpty())
			{
				Text.Font = (GameFont)2;
				var nameSize = Text.CalcSize(title);
				var startingX = (inRect.width/2) - (nameSize.x/2);
				Widgets.Label(new Rect(startingX, num, inRect.width - startingX, 42f), title);
				num += 42f;
			}
			Text.Font = GameFont.Small;
			if (image != null)
			{
				var startingX = (inRect.width/2) - (image.width * 0.5f);
				Widgets.ButtonImage(new Rect(startingX, num, inRect.width - startingX, image.height), image, Color.white, Color.white);
				num += image.height;
				num += 42f;
			}
			Text.Font = GameFont.Small;
			Rect outRect = new Rect(inRect.x, num, inRect.width, inRect.height - 35f - 5f - num);
			float width = outRect.width - 16f;
			Rect viewRect = new Rect(0f, num, width, Text.CalcHeight(text, width));
			Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect, true);
			Widgets.Label(new Rect(0f, num, viewRect.width, viewRect.height), text);
			Widgets.EndScrollView();
			int num2 = (!buttonCText.NullOrEmpty()) ? 3 : 2;
			float num3 = inRect.width / (float)num2;
			float width2 = num3 - 20f;
			if (buttonADestructive)
			{
				GUI.color = new Color(1f, 0.3f, 0.35f);
			}
			string label = (!get_InteractionDelayExpired()) ? (buttonAText + "(" + Mathf.Ceil(get_TimeUntilInteractive()).ToString("F0") + ")") : buttonAText;
			if (Widgets.ButtonText(new Rect(num3 * (float)(num2 - 1) + 10f, inRect.height - 35f, width2, 35f), label, true, false, true))
			{
				if (get_InteractionDelayExpired())
				{
					Close(true);
				}
			}
			GUI.color = Color.white;
		}

		public override void OnCancelKeyPressed()
		{
			if (cancelAction != null)
			{
				cancelAction();
				Close(true);
			}
			else
			{
				base.OnCancelKeyPressed();
			}
		}

		public override void OnAcceptKeyPressed()
		{
			if (acceptAction != null)
			{
				acceptAction();
				Close(true);
			}
			else
			{
				base.OnAcceptKeyPressed();
			}
		}

		private static void CreateConfirmation()
		{
		}
	}
}
