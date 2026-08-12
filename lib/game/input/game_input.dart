enum GameAction { throttle, brake, drift, nitro, pause }

class GameInputSnapshot {
  const GameInputSnapshot({
    required this.steering,
    required this.throttle,
    required this.brake,
    required this.drift,
    required this.nitro,
    required this.pausePressed,
  });

  final double steering;
  final double throttle;
  final double brake;
  final bool drift;
  final bool nitro;
  final bool pausePressed;
}

class GameInputState {
  double _steering = 0;
  double _throttle = 0;
  double _brake = 0;
  bool _drift = false;
  bool _nitro = false;
  bool _pausePressed = false;

  void setSteering(double value) {
    _steering = value.clamp(-1.0, 1.0);
  }

  void setThrottle(double value) {
    _throttle = value.clamp(0.0, 1.0);
  }

  void setBrake(double value) {
    _brake = value.clamp(0.0, 1.0);
  }

  void setAction(GameAction action, bool pressed) {
    switch (action) {
      case GameAction.throttle:
        setThrottle(pressed ? 1 : 0);
      case GameAction.brake:
        setBrake(pressed ? 1 : 0);
      case GameAction.drift:
        _drift = pressed;
      case GameAction.nitro:
        _nitro = pressed;
      case GameAction.pause:
        if (pressed) {
          _pausePressed = true;
        }
    }
  }

  GameInputSnapshot consumeSnapshot() {
    final snapshot = GameInputSnapshot(
      steering: _steering,
      throttle: _throttle,
      brake: _brake,
      drift: _drift,
      nitro: _nitro,
      pausePressed: _pausePressed,
    );
    _pausePressed = false;
    return snapshot;
  }

  void reset() {
    _steering = 0;
    _throttle = 0;
    _brake = 0;
    _drift = false;
    _nitro = false;
    _pausePressed = false;
  }
}
