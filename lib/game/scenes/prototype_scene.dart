import 'package:flame/components.dart';

class PrototypeScene extends Component {
  PrototypeScene({required this.trackId});

  final String trackId;

  @override
  Future<void> onLoad() async {
    await super.onLoad();
    // Scene composition is intentionally thin at this stage. Track, vehicle,
    // camera and VFX components are added by their dedicated P1 tasks.
  }
}
