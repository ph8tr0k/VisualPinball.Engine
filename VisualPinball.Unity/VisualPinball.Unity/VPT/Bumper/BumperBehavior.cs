#region ReSharper
// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
#endregion

using Unity.Entities;
using UnityEngine;
using VisualPinball.Engine.VPT.Bumper;
using VisualPinball.Unity.Extensions;
using VisualPinball.Unity.Game;

namespace VisualPinball.Unity.VPT.Bumper
{
	[ExecuteAlways]
	[AddComponentMenu("Visual Pinball/Bumper")]
	public class BumperBehavior : ItemBehavior<Engine.VPT.Bumper.Bumper, BumperData>, IConvertGameObjectToEntity
	{
		protected override string[] Children => new []{"Base", "Cap", "Ring", "Skirt"};

		protected override Engine.VPT.Bumper.Bumper GetItem() => new Engine.VPT.Bumper.Bumper(data);

		private void OnDestroy()
		{
			if (!Application.isPlaying) {
				_table.Remove<Engine.VPT.Bumper.Bumper>(Name);
			}
		}

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			Convert(entity, dstManager);
			dstManager.AddComponentData(entity, new BumperStaticData {
				Force = data.Force,
				HitEvent = data.HitEvent,
				Threshold = data.Threshold
			});

			transform.GetComponentInParent<Player>().RegisterBumper(Item, entity, gameObject);
		}

		public override ItemDataTransformType EditorPositionType => ItemDataTransformType.TwoD;
		public override Vector3 GetEditorPosition() => data.Center.ToUnityVector3(0f);
		public override void SetEditorPosition(Vector3 pos) => data.Center = pos.ToVertex2Dxy();

		public override ItemDataTransformType EditorRotationType => ItemDataTransformType.OneD;
		public override Vector3 GetEditorRotation() => new Vector3(data.Orientation, 0, 0);
		public override void SetEditorRotation(Vector3 rot) => data.Orientation = rot.x;

		public override ItemDataTransformType EditorScaleType => ItemDataTransformType.OneD;
		public override Vector3 GetEditorScale() => new Vector3(data.Radius, 0f, 0f);
		public override void SetEditorScale(Vector3 scale) => data.Radius = scale.x;
	}
}
