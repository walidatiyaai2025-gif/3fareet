import 'package:flutter/foundation.dart';

enum FrontEndStage { splash, mainMenu, modeSelection, loading, racing, error }

class FrontEndFlowController extends ChangeNotifier {
  FrontEndStage _stage = FrontEndStage.splash;
  bool _isArabic = false;
  String? _errorMessage;

  FrontEndStage get stage => _stage;
  bool get isArabic => _isArabic;
  String? get errorMessage => _errorMessage;

  void showMainMenu() {
    _errorMessage = null;
    _setStage(FrontEndStage.mainMenu);
  }

  void showModeSelection() {
    _setStage(FrontEndStage.modeSelection);
  }

  void showLoading() {
    _setStage(FrontEndStage.loading);
  }

  void enterRace() {
    _errorMessage = null;
    _setStage(FrontEndStage.racing);
  }

  void showError(String message) {
    _errorMessage = message.trim().isEmpty ? 'Unknown error' : message.trim();
    _setStage(FrontEndStage.error);
  }

  void retry() {
    showMainMenu();
  }

  void toggleLanguage() {
    _isArabic = !_isArabic;
    notifyListeners();
  }

  void _setStage(FrontEndStage value) {
    if (_stage == value) {
      return;
    }
    _stage = value;
    notifyListeners();
  }
}
