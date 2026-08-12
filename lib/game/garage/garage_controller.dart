import 'package:afareet_asphalt/game/garage/car_catalog.dart';
import 'package:flutter/foundation.dart';

class GarageLoadout {
  const GarageLoadout({
    required this.vehicleId,
    required this.paintId,
    required this.wheelId,
    required this.magicTrailId,
    required this.spiritCosmeticId,
  });

  final String vehicleId;
  final String paintId;
  final String wheelId;
  final String magicTrailId;
  final String spiritCosmeticId;

  GarageLoadout copyWith({
    String? vehicleId,
    String? paintId,
    String? wheelId,
    String? magicTrailId,
    String? spiritCosmeticId,
  }) {
    return GarageLoadout(
      vehicleId: vehicleId ?? this.vehicleId,
      paintId: paintId ?? this.paintId,
      wheelId: wheelId ?? this.wheelId,
      magicTrailId: magicTrailId ?? this.magicTrailId,
      spiritCosmeticId: spiritCosmeticId ?? this.spiritCosmeticId,
    );
  }
}

class GarageVehicleView {
  const GarageVehicleView({
    required this.entry,
    required this.unlocked,
    required this.selected,
    required this.equipped,
  });

  final CarCatalogEntry entry;
  final bool unlocked;
  final bool selected;
  final bool equipped;
}

class GaragePreviewModel {
  const GaragePreviewModel({
    required this.vehicleId,
    required this.displayName,
    required this.paintId,
    required this.wheelId,
    required this.magicTrailId,
    required this.spiritCosmeticId,
    this.assetPath,
  });

  final String vehicleId;
  final String displayName;
  final String paintId;
  final String wheelId;
  final String magicTrailId;
  final String spiritCosmeticId;
  final String? assetPath;
}

class GarageController extends ChangeNotifier {
  GarageController({
    required this.catalog,
    required int playerLevel,
    this.paintOptions = const <String>['factory', 'neon_cyan', 'desert_gold'],
    this.wheelOptions = const <String>['street', 'mesh_black', 'spirit_forged'],
    this.magicTrailOptions = const <String>['none', 'cyan_smoke', 'gold_sparks'],
    this.spiritCosmeticOptions = const <String>['none', 'scarab', 'djinn_eye'],
  }) : _playerLevel = playerLevel.clamp(1, 999) {
    if (catalog.entries.isEmpty) {
      throw ArgumentError('Garage requires at least one catalog vehicle.');
    }

    final firstUnlocked = catalog.entries.cast<CarCatalogEntry?>().firstWhere(
          (entry) => entry!.unlockLevel <= _playerLevel,
          orElse: () => null,
        );
    _selectedVehicleId = (firstUnlocked ?? catalog.entries.first).id;
    final entry = selectedVehicle;
    _loadouts[entry.id] = _defaultLoadout(entry);
    if (isUnlocked(entry.id)) {
      _equippedVehicleId = entry.id;
    }
  }

  final CarCatalog catalog;
  final List<String> paintOptions;
  final List<String> wheelOptions;
  final List<String> magicTrailOptions;
  final List<String> spiritCosmeticOptions;
  final Map<String, GarageLoadout> _loadouts = <String, GarageLoadout>{};

  late int _playerLevel;
  late String _selectedVehicleId;
  String? _equippedVehicleId;

  int get playerLevel => _playerLevel;
  String get selectedVehicleId => _selectedVehicleId;
  String? get equippedVehicleId => _equippedVehicleId;

  CarCatalogEntry get selectedVehicle => catalog.byId(_selectedVehicleId)!;

  GarageLoadout get selectedLoadout =>
      _loadouts.putIfAbsent(selectedVehicle.id, () => _defaultLoadout(selectedVehicle));

  List<GarageVehicleView> get vehicles => catalog.entries
      .map(
        (entry) => GarageVehicleView(
          entry: entry,
          unlocked: isUnlocked(entry.id),
          selected: entry.id == _selectedVehicleId,
          equipped: entry.id == _equippedVehicleId,
        ),
      )
      .toList(growable: false);

  bool isUnlocked(String vehicleId) {
    final entry = catalog.byId(vehicleId);
    return entry != null && entry.unlockLevel <= _playerLevel;
  }

  void setPlayerLevel(int level) {
    final next = level.clamp(1, 999);
    if (next == _playerLevel) {
      return;
    }
    _playerLevel = next;
    notifyListeners();
  }

  bool selectVehicle(String vehicleId) {
    final entry = catalog.byId(vehicleId);
    if (entry == null || vehicleId == _selectedVehicleId) {
      return entry != null;
    }
    _selectedVehicleId = vehicleId;
    _loadouts.putIfAbsent(entry.id, () => _defaultLoadout(entry));
    notifyListeners();
    return true;
  }

  bool equipSelected() {
    if (!isUnlocked(_selectedVehicleId)) {
      return false;
    }
    if (_equippedVehicleId == _selectedVehicleId) {
      return true;
    }
    _equippedVehicleId = _selectedVehicleId;
    notifyListeners();
    return true;
  }

  bool setPaint(String id) => _setCustomization(
        id: id,
        allowed: paintOptions,
        apply: (loadout) => loadout.copyWith(paintId: id),
      );

  bool setWheel(String id) => _setCustomization(
        id: id,
        allowed: wheelOptions,
        apply: (loadout) => loadout.copyWith(wheelId: id),
      );

  bool setMagicTrail(String id) => _setCustomization(
        id: id,
        allowed: magicTrailOptions,
        apply: (loadout) => loadout.copyWith(magicTrailId: id),
      );

  bool setSpiritCosmetic(String id) => _setCustomization(
        id: id,
        allowed: spiritCosmeticOptions,
        apply: (loadout) => loadout.copyWith(spiritCosmeticId: id),
      );

  Map<String, double> get normalizedStats => <String, double>{
        'Top Speed': selectedVehicle.stats.topSpeed / 100,
        'Acceleration': selectedVehicle.stats.acceleration / 100,
        'Handling': selectedVehicle.stats.handling / 100,
        'Nitro': selectedVehicle.stats.nitro / 100,
      };

  GaragePreviewModel get preview {
    final entry = selectedVehicle;
    final loadout = selectedLoadout;
    return GaragePreviewModel(
      vehicleId: entry.id,
      displayName: entry.displayName,
      assetPath: entry.previewAsset,
      paintId: loadout.paintId,
      wheelId: loadout.wheelId,
      magicTrailId: loadout.magicTrailId,
      spiritCosmeticId: loadout.spiritCosmeticId,
    );
  }

  bool _setCustomization({
    required String id,
    required List<String> allowed,
    required GarageLoadout Function(GarageLoadout current) apply,
  }) {
    if (!isUnlocked(_selectedVehicleId) || !allowed.contains(id)) {
      return false;
    }
    _loadouts[_selectedVehicleId] = apply(selectedLoadout);
    notifyListeners();
    return true;
  }

  GarageLoadout _defaultLoadout(CarCatalogEntry entry) {
    return GarageLoadout(
      vehicleId: entry.id,
      paintId: paintOptions.contains(entry.basePaintId)
          ? entry.basePaintId
          : paintOptions.first,
      wheelId: wheelOptions.first,
      magicTrailId: magicTrailOptions.first,
      spiritCosmeticId: spiritCosmeticOptions.first,
    );
  }
}
