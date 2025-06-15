# The concept
State machines are awesome!  But long, messy switch statements?  Not so much.

To implement state machines in an exensible and encapsulated way, this project
uses a custom state machine system.  The basic premise is:
* A `StateMachine` node keeps a cache of `IState` objects, each of which has the
    following methods:
    * `OnStateEntered()`
    * `OnStateExited()`
    * `_PhysicsProcess()`
    * `_Process()`
* Whenever `StateMachine` changes states, it:
    * Calls `OnStateExited()` on the current state
    * Retrives the new state from the cache, creating a new instance if there
        isn't one already
    * Sets `CurrentState` to the new state
    * Calls `OnStateEntered()` on the new state

`IState` is deliberately designed to resemble a Godot node.  Inside the
`_PhysicsProcess()` method, you'd put logic that controls the subject, defining
what it should do in this state.

# Using `StateMachine`

First, some terminology.  In the context of state machines, the word "subject"
refers to the node being controlled by a state machine.

# Example: A blinking 

```C#
public partial class Blinker : Node3D
{
    private readonly StateMachine _stateMachine = new StateMachine();

    public override void _Ready()
    {
        AddChild(_stateMachine);
        _stateMachine.ChangeState<BlinkOn>();
    }

    private class BlinkOn : State<Blinker>
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

    private class BlinkOff : State<Blinker>
    {
        private float _timer;

        public override void OnStateEntered()
        {
            _timer = 0.25f;
            Self.Visible = false; // Self is a Blinker
        }

        public override void OnStateExited()
        {
            Self.Visible = true;
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