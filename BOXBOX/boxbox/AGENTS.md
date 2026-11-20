## Regras de Assets (Godot)

- Nunca faça hardcode de caminhos individuais de modelos `.glb` ou `.tscn` no código de geração procedural.
- Sempre verifique o conteúdo completo de `res://Assets/` antes de criar categorias de spawn.
- Use `ResourceLoader.ListDirectory()` para descobrir recursos importados em runtime e em builds exportados; ordene os resultados antes de qualquer seleção determinística.
- Toda variação admissível encontrada em uma categoria deve participar do sorteio dessa categoria.
- Mantenha peças topológicas (ruas, rampas e cruzamentos) separadas de variações puramente visuais.
