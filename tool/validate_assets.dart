import 'dart:io';

final _approvedRoots = <String>[
  'assets/cars/',
  'assets/tracks/',
  'assets/environment/',
  'assets/vfx/',
  'assets/audio/music/',
  'assets/audio/sfx/',
  'assets/ui/',
  'assets/placeholders/',
];

final _namePattern = RegExp(r'^[a-z0-9_./-]+$');

void main(List<String> args) {
  final root = Directory(args.isEmpty ? 'assets' : args.first);
  if (!root.existsSync()) {
    stderr.writeln('Asset root not found: ${root.path}');
    exitCode = 2;
    return;
  }

  final violations = <String>[];
  for (final entity in root.listSync(recursive: true, followLinks: false)) {
    if (entity is! File) continue;
    final path = entity.path.replaceAll('\\', '/');
    if (path.endsWith('/.gitkeep') || path.endsWith('/README.md')) continue;
    if (!_approvedRoots.any(path.startsWith)) {
      violations.add('$path: outside approved runtime folders');
    }
    final basename = path.split('/').last;
    if (!_namePattern.hasMatch(basename)) {
      violations.add('$path: invalid filename; use lowercase snake_case ASCII');
    }
    if (path.contains('/placeholders/') && !basename.contains('_placeholder_')) {
      violations.add('$path: placeholder filename must contain _placeholder_');
    }
    if (!path.contains('/placeholders/') && basename.contains('_placeholder_')) {
      violations.add('$path: placeholder-tagged file must live under assets/placeholders/');
    }
  }

  if (violations.isNotEmpty) {
    for (final violation in violations) {
      stderr.writeln(violation);
    }
    exitCode = 1;
    return;
  }
  stdout.writeln('Asset validation passed.');
}
