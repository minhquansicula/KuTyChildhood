const fs = require('fs');
const path = require('path');

const source = process.argv[2];
const destination = process.argv[3];
if (!source || !destination) {
  throw new Error('Usage: node extract-glb-textures.js <source.glb> <destination>');
}

const data = fs.readFileSync(source);
if (data.toString('ascii', 0, 4) !== 'glTF') {
  throw new Error(`${source} is not a binary glTF file.`);
}

let json;
let binary;
let offset = 12;
while (offset + 8 <= data.length) {
  const length = data.readUInt32LE(offset);
  const type = data.readUInt32LE(offset + 4);
  const content = data.subarray(offset + 8, offset + 8 + length);
  if (type === 0x4e4f534a) json = JSON.parse(content.toString('utf8').replace(/\0+$/g, '').trim());
  if (type === 0x004e4942) binary = content;
  offset += 8 + length;
}
if (!json || !binary) throw new Error('GLB JSON or BIN chunk is missing.');

fs.mkdirSync(destination, { recursive: true });
const imageFiles = [];
for (let i = 0; i < (json.images || []).length; i++) {
  const image = json.images[i];
  if (image.bufferView === undefined) continue;
  const view = json.bufferViews[image.bufferView];
  const start = (view.byteOffset || 0);
  const bytes = binary.subarray(start, start + view.byteLength);
  const ext = image.mimeType === 'image/jpeg' ? '.jpg' : image.mimeType === 'image/webp' ? '.webp' : '.png';
  const rawName = image.name || `Image_${i}`;
  const safeName = rawName.replace(/[^a-zA-Z0-9_-]+/g, '_');
  const fileName = `${safeName}${ext}`;
  fs.writeFileSync(path.join(destination, fileName), bytes);
  imageFiles[i] = fileName;
}

const textureToImage = (textureIndex) => {
  if (textureIndex === undefined || !json.textures || !json.textures[textureIndex]) return null;
  return imageFiles[json.textures[textureIndex].source] || null;
};
const materials = (json.materials || []).map((material, index) => {
  const pbr = material.pbrMetallicRoughness || {};
  return {
    index,
    name: material.name || `Material_${index}`,
    baseColorFactor: pbr.baseColorFactor || [1, 1, 1, 1],
    baseColorTexture: textureToImage(pbr.baseColorTexture && pbr.baseColorTexture.index),
    metallicFactor: pbr.metallicFactor === undefined ? 1 : pbr.metallicFactor,
    roughnessFactor: pbr.roughnessFactor === undefined ? 1 : pbr.roughnessFactor,
    metallicRoughnessTexture: textureToImage(pbr.metallicRoughnessTexture && pbr.metallicRoughnessTexture.index),
    normalTexture: textureToImage(material.normalTexture && material.normalTexture.index),
    occlusionTexture: textureToImage(material.occlusionTexture && material.occlusionTexture.index),
    emissiveTexture: textureToImage(material.emissiveTexture && material.emissiveTexture.index),
    emissiveFactor: material.emissiveFactor || [0, 0, 0]
  };
});

const report = { source, images: imageFiles.filter(Boolean), materials };
fs.writeFileSync(path.join(destination, 'glb-material-map.json'), JSON.stringify(report, null, 2));
process.stdout.write(JSON.stringify(report, null, 2));
