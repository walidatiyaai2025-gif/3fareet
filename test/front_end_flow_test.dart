import 'package:afareet_asphalt/game/ui/front_end_flow.dart';
import 'package:afareet_asphalt/game/ui/ui_tokens.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('FrontEndFlowController', () {
    test('covers menu, mode, loading, race and retry states', () {
      final flow = FrontEndFlowController();
      expect(flow.stage, FrontEndStage.splash);

      flow.showMainMenu();
      expect(flow.stage, FrontEndStage.mainMenu);
      flow.showModeSelection();
      expect(flow.stage, FrontEndStage.modeSelection);
      flow.showLoading();
      expect(flow.stage, FrontEndStage.loading);
      flow.enterRace();
      expect(flow.stage, FrontEndStage.racing);
      flow.showError('network');
      expect(flow.stage, FrontEndStage.error);
      expect(flow.errorMessage, 'network');
      flow.retry();
      expect(flow.stage, FrontEndStage.mainMenu);
    });

    test('supports RTL language toggle', () {
      final flow = FrontEndFlowController();
      expect(flow.isArabic, isFalse);
      flow.toggleLanguage();
      expect(flow.isArabic, isTrue);
      flow.toggleLanguage();
      expect(flow.isArabic, isFalse);
    });
  });

  test('accessibility text scale stays within supported HUD bounds', () {
    expect(AfareetUiTokens.clampTextScale(0.2), 0.85);
    expect(AfareetUiTokens.clampTextScale(1.1), 1.1);
    expect(AfareetUiTokens.clampTextScale(4), 1.35);
  });
}
