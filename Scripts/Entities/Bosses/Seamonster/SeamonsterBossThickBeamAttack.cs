using System;
using Godot;

namespace FastDragon
{
    public partial class SeamonsterBoss
    {
        [ExportGroup("Attacks/Thick Beam")]
        [Export] public float ThickBeamDuration = 5f;
        [Export] public float ThickBeamRadius = 0.5f;
        [Export] public float ThickBeamElongateSpeed = 30;
        [Export] public float ThickBeamTargetMoveSpeed = 2.5f;
        [Export] public float ThickBeamStartDelay = 0.75f;

        private ThickBeam _thickBeam => GetNode<ThickBeam>("%ThickBeam");

        private partial class ThickBeamAttack : SeamonsterBossState
        {
            private float _timer;

            public override void OnStateEntered()
            {
                _timer = 0;

                _self._thickBeam.DamagedPlayer += OnDealtDamageToPlayer;
                _self._thickBeam.TargetPos = PlayerPos();
                _self._thickBeam.Radius = _self.ThickBeamRadius;
                _self._thickBeam.MaxLength = 0;
            }

            public override void OnStateExited()
            {
                _self._thickBeam.Visible = false;
                _self._thickBeam.DamageEnabled = false;
                _self._thickBeam.DamagedPlayer -= OnDealtDamageToPlayer;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                _timer += delta;

                UpdateThickBeam(delta);

                if (_self.AllPowerOrbsBroken())
                {
                    ChangeState<Vulnerable>();
                    return;
                }

                if (_timer >= _self.ThickBeamDuration)
                {
                    ChangeState<Submerging>();
                    return;
                }
            }

            private void UpdateThickBeam(float delta)
            {
                if (_timer >= _self.ThickBeamStartDelay)
                {
                    _self._thickBeam.Visible = true;
                    _self._thickBeam.DamageEnabled = true;

                    _self._thickBeam.MaxLength += _self.ThickBeamElongateSpeed * delta;

                    _self._thickBeam.TargetPos = _self._thickBeam.TargetPos.MoveToward(
                        PlayerPos(),
                        _self.ThickBeamTargetMoveSpeed * delta
                    );
                }
            }

            private void OnDealtDamageToPlayer()
            {
                ChangeState<Laughing>();
            }

            private Vector3 PlayerPos()
            {
                return GetTree().FindNode<Player>()
                    .GlobalPosition
                    .Flattened();
            }
        }

    }
}