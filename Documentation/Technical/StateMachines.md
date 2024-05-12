# The concept
State machines are awesome!  But long, messy switch statements?  Not so much.

To implement state machines in an exensible and encapsulated way, this project
uses a custom state machine system.  The basic premise is:
* Every state is a node, which controls its parent from its `_PhysicsProcess()`
* The current state has its `ProcessMode` set to `Inherit`
* All other states have their `ProcessMode` set to `Disabled`
* To change states, we:
    * Call `OnStateExited()` on the current state
    * Set the current state's `ProcessMode` to `Disabled`
    * Create an instance of the new state and add it as a child, if one does
        not already exist
    * Set the new state's `ProcessMode` to `Inherit`
    * Call `OnStateEntered()` on the new state

This technique is implemented using two classes:
* The abstract `State` class, which all state nodes inherit from.
* The `StateMachine` node, which all state nodes are a child of.

When I said that states control their parents earlier, I lied.  They actually
control their `StateMachine`'s parent.

# Using `StateMachine`

First, some terminology.  In the context of state machines, the word "subject"
refers to the node being controlled by a state machine.

# Example: A blinking 

```C#
public partial class Blinker : Node3D
{
    private readonly StateMachine _stateMachine = new StateMachine(typeof(BlinkerState));

    public override void _Ready()
    {
        AddChild(_stateMachine);
        _stateMachine.ChangeState<BlinkOn>();
    }

    private abstract partial class BlinkerState : State
    {
        protected Blinker _self => _stateMachine.GetParent<Blinker>();
    }

    private partial class BlinkOn : SomeEntityState
    {
        private float _timer;

        public override void OnStateEntered()
        {
            _timer = 0.5f;
        }

        public override void _PhysicsProcess(double deltaD)
        {
            _timer -= (float)deltaD;

            if (_timer <= 0)
                ChangeState<BlinkOff>();
        }
    }

    private partial class BlinkOff : SomeEntityState
    {
        private float _timer;

        public override void OnStateEntered()
        {
            _timer = 0.25f;
            _self.Visible = false;
        }

        public override void OnStateExited()
        {
            _self.Visible = true;
            // Best practice: If a state changes a property away from its
            // "normal" value, that same state is responsible for cleaning up
            // after itself in OnStateExited().
            //
            // In this example, Visible's "normal" value is true, which is why
            // we're setting it to true _here_ instead of in
            // BlinkOn.OnStateEntered()
        }

        public override void _PhysicsProcess(double deltaD)
        {
            _timer -= (float)deltaD;

            if (_timer <= 0)
                ChangeState<BlinkOn>();
        }
    }
}
```

# Special case: `Player`
In most cases, a subject's states are all private nested classes, stored within
the same file as the subject's main script.  This allows states to access
private members on the subject, without needing to expose those members to the
rest of the game.

The player, on the other hand, is a different beast.  The player has so many
states that they needed to be split up into separate files.  Because they were
no longer in the same file, they couldn't access private members anymore*.  As
a result:
* Any logic reused between player states must be implemented as a `protected`
    helper method on the abstract `PlayerState` class, instead of being private
    on the `Player` class.
* Player states need to be mostly independent of each other, since they cannot
    use the player's private variables to share timers/counters with each other.

*Technically, I _could_ have used C#'s "partial classes" feature to split the
states out into separate files while still letting them be nested in the
`Player` class, but something about that felt "wrong" to me, so I didn't do it.