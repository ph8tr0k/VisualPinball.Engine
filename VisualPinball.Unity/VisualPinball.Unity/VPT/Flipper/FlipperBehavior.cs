﻿#region ReSharper
// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
#endregion

using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using VisualPinball.Unity.VPT.Table;
using VisualPinball.Engine.VPT.Flipper;
using VisualPinball.Unity.Game;

namespace VisualPinball.Unity.VPT.Flipper
{
	[RequiresEntityConversion]
	[AddComponentMenu("Visual Pinball/Flipper")]
	public class FlipperBehavior : ItemBehavior<Engine.VPT.Flipper.Flipper, FlipperData>, IConvertGameObjectToEntity
	{
		protected override string[] Children => new []{ FlipperMeshGenerator.BaseName, FlipperMeshGenerator.RubberName };

		protected override Engine.VPT.Flipper.Flipper GetItem()
		{
			return new Engine.VPT.Flipper.Flipper(data);
		}

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			Convert(entity, dstManager);
			var d = GetMaterialData();
			dstManager.AddComponentData(entity, d);
			dstManager.AddComponentData(entity, GetMovementData(d));
			dstManager.AddComponentData(entity, GetVelocityData(d));
			dstManager.AddComponentData(entity, GetHitData());
			dstManager.AddComponentData(entity, new SolenoidStateData { Value = false });

			// register
			transform.GetComponentInParent<Player>().RegisterFlipper(Item, entity, gameObject);
		}

		private void Awake()
		{
			var rootObj = gameObject.transform.GetComponentInParent<TableBehavior>();
			// can be null in editor, shouldn't be at runtime.
			if (rootObj != null)
			{
				_tableData = rootObj.data;
			}
		}

		private FlipperStaticData GetMaterialData()
		{
			float flipperRadius;
			if (data.FlipperRadiusMin > 0 && data.FlipperRadiusMax > data.FlipperRadiusMin) {
				flipperRadius = data.FlipperRadiusMax - (data.FlipperRadiusMax - data.FlipperRadiusMin) /* m_ptable->m_globalDifficulty*/;
				flipperRadius = math.max(flipperRadius, data.BaseRadius - data.EndRadius + 0.05f);

			} else {
				flipperRadius = data.FlipperRadiusMax;
			}

			var endRadius = math.max(data.EndRadius, 0.01f); // radius of flipper end
			flipperRadius = math.max(flipperRadius, 0.01f); // radius of flipper arc, center-to-center radius
			var angleStart = math.radians(data.StartAngle);
			var angleEnd = math.radians(data.EndAngle);

			if (angleEnd == angleStart) {
				// otherwise hangs forever in collisions/updates
				angleEnd += 0.0001f;
			}

			// model inertia of flipper as that of rod of length flipper around its end
			var mass = data.GetFlipperMass(_tableData);
			var inertia = (float) (1.0 / 3.0) * mass * (flipperRadius * flipperRadius);

			return new FlipperStaticData {
				Inertia = inertia,
				AngleStart = angleStart,
				AngleEnd = angleEnd,
				Strength = data.GetStrength(_tableData),
				ReturnRatio = data.GetReturnRatio(_tableData),
				TorqueDamping = data.GetTorqueDamping(_tableData),
				TorqueDampingAngle = data.GetTorqueDampingAngle(_tableData),
				RampUpSpeed = data.GetRampUpSpeed(_tableData),

				EndRadius = endRadius,
				FlipperRadius = flipperRadius
			};
		}

		private FlipperMovementData GetMovementData(FlipperStaticData d)
		{
			// store flipper base rotation without starting angle
			var baseRotation = math.normalize(math.mul(
				math.normalize(transform.rotation),
				quaternion.EulerXYZ(0, 0, -d.AngleStart)
			));
			return new FlipperMovementData {
				Angle = d.AngleStart,
				AngleSpeed = 0f,
				AngularMomentum = 0f,
				EnableRotateEvent = 0,
				BaseRotation = baseRotation,
			};
		}

		private static FlipperVelocityData GetVelocityData(FlipperStaticData d)
		{
			return new FlipperVelocityData {
				AngularAcceleration = 0f,
				ContactTorque = 0f,
				CurrentTorque = 0f,
				Direction = d.AngleEnd >= d.AngleStart,
				IsInContact = false
			};
		}

		private FlipperHitData GetHitData()
		{
			var ratio = (math.max(data.BaseRadius, 0.01f) - math.max(data.EndRadius, 0.01f)) / math.max(data.FlipperRadius, 0.01f);
			var zeroAngNorm = new float2(
				math.sqrt(1.0f - ratio * ratio), // F2 Norm, used in Green's transform, in FPM time search  // =  sinf(faceNormOffset)
				-ratio                              // F1 norm, change sign of x component, i.e -zeroAngNorm.x // = -cosf(faceNormOffset)
			);

			return new FlipperHitData {
				ZeroAngNorm = zeroAngNorm,
				HitMomentBit = true,
				HitVelocity = new float2(),
				LastHitFace = false,
			};
		}

		protected virtual void OnDrawGizmos()
		{
			// flippers tend to have sub object meshes, so nothing would be pickable on this game object,
			// but generally you'll want to manipulate the whole flipper, so we'll draw an invisible
			// gizmo slightly larger than one of the child meshes so clicking on the flipper in editor
			// selects this object
			var mf = this.GetComponentInChildren<MeshFilter>();
			if (mf != null && mf.sharedMesh != null) {
				Gizmos.color = Color.clear;
				Gizmos.DrawMesh(mf.sharedMesh, transform.position, transform.rotation, transform.lossyScale * 1.1f);
			}
		}
	}
}
