import 'package:afareet_asphalt/game/career/chapter_one.dart';
import 'package:afareet_asphalt/game/garage/default_car_catalog.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('prototype catalog and career foundation are boot-valid', () {
    final catalog = buildDefaultCarCatalog();
    final chapter = buildChapterOneFoundation();

    expect(validateCarCatalog(catalog), isEmpty);
    expect(catalog.entries.length, 4);
    expect(chapter.isValid, isTrue);
    expect(chapter.nodes.length, greaterThanOrEqualTo(5));
  });
}
