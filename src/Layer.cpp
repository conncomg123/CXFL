#include "../include/Layer.h"
#include <algorithm>
#include <stdexcept>
void Layer::loadFrames(pugi::xml_node& layerNode) noexcept {
	auto frames = layerNode.child("frames").children("DOMFrame");
	for (auto iter = frames.begin(); iter != frames.end(); ++iter) {
		this->frames.push_back(std::make_unique<Frame>(*iter));
	}
}
Layer::Layer(pugi::xml_node& layerNode) noexcept {
	this->root = layerNode;
	this->setColor(layerNode.attribute("color").empty() ? "#000000" : layerNode.attribute("color").value());
	this->setLayerType(layerNode.attribute("layerType").empty() ? "normal" : layerNode.attribute("layerType").value());
	this->setLocked(layerNode.attribute("locked").as_bool());
	this->setName(layerNode.attribute("name").empty() ? "layer" : layerNode.attribute("name").value());
	this->setParentLayerIndex((layerNode.attribute("parentLayerIndex").empty()) ? std::nullopt : std::make_optional<unsigned int>(layerNode.attribute("parentLayerIndex").as_uint()));
	loadFrames(this->root);
}
Layer::~Layer() noexcept {

}

void Layer::removeKeyframe(unsigned int keyFrameIndex) noexcept {
	Frame* frame = this->frames[keyFrameIndex].get();
	this->getRoot().child("frames").remove_child(frame->getRoot());
	this->frames.erase(this->frames.begin() + keyFrameIndex);
}

bool Layer::clearKeyFrame(unsigned int frameIndex) noexcept {
	unsigned int index = this->getKeyframeIndex(frameIndex);
	Frame* frame = this->frames[index].get();
	if (frameIndex != frame->getStartFrame()) return false;
	// Special case for if there's one keyframe: replace it with a blank one (remove all elements)
	if (this->frames.size() == 1) {
		if(frame->isEmpty()) return false;
		frame->clearElements();
		return true;
	}
	// Special case for the first keyframe: replace it with the next one
	if(index == 0) {
		Frame* nextFrame = this->frames[index + 1].get();
		nextFrame->setDuration(nextFrame->getDuration() + frame->getDuration());
		nextFrame->setStartFrame(index);
		this->removeKeyframe(index);
		return true;
	}
	Frame* prevFrame = this->frames[index - 1].get();
	prevFrame->setDuration(prevFrame->getDuration() + frame->getDuration());
	this->removeKeyframe(index);
	return true;
}

bool Layer::insertKeyframe(unsigned int frameIndex, bool isBlank) noexcept {
	unsigned int index = this->getKeyframeIndex(frameIndex);
	Frame* frame = this->frames[index].get();
	if (frameIndex == frame->getStartFrame()) {
		frameIndex++;
		if (frameIndex >= this->getFrameCount()) return false;
		unsigned int newIndex = this->getKeyframeIndex(frameIndex);
		if (newIndex == index) return false;
	}
	auto newFrame = std::make_unique<Frame>(*frame, isBlank);
	newFrame->setName("");
	newFrame->setDuration(frame->getDuration() + frame->getStartFrame() - frameIndex);
	frame->setDuration(frameIndex - frame->getStartFrame());
	newFrame->setStartFrame(frameIndex);
	this->frames.insert(this->frames.begin() + index + 1, std::move(newFrame));
	return true;
}

bool Layer::insertKeyframe(unsigned int frameIndex) noexcept {
	return this->insertKeyframe(frameIndex, false);
}

bool Layer::insertBlankKeyframe(unsigned int frameIndex) noexcept {
	return this->insertKeyframe(frameIndex, true);
}

Frame* Layer::getKeyFrame(unsigned int index) const noexcept {
	return frames[index].get();
}

constexpr unsigned int Layer::getKeyframeIndex(unsigned int frameIndex) const noexcept {
	// return the nth keyframe where n.startFrame <= frameIndex < n.startFrame + n.duration using binary search
	unsigned int index = 0;
	unsigned int start = 0;
	unsigned int end = this->frames.size() - 1;
	while (start <= end) {
		unsigned int mid = (start + end) / 2;
		Frame* frame = this->frames[mid].get();
		if (frame->getStartFrame() <= frameIndex && frameIndex < frame->getStartFrame() + frame->getDuration()) {
			index = mid;
			break;
		}
		else if (frame->getStartFrame() > frameIndex) {
			end = mid - 1;
		}
		else {
			start = mid + 1;
		}
	}
	return index;
}

Frame* Layer::getFrame(unsigned int frameIndex) const noexcept {
	unsigned int index = this->getKeyframeIndex(frameIndex);
	return this->frames[index].get();
}

const std::string& Layer::getColor() const noexcept {
	return this->color;
}
void Layer::setColor(const std::string& color) noexcept {
	if (this->root.attribute("color").empty()) this->root.append_attribute("color");
	this->root.attribute("color").set_value(color.c_str());
	this->color = color;
}
const std::string& Layer::getLayerType() const noexcept {
	return this->layerType;
}
void Layer::setLayerType(const std::string& layerType) noexcept(false) {
	if (std::find(ACCEPTABLE_LAYER_TYPES.begin(), ACCEPTABLE_LAYER_TYPES.end(), layerType) == ACCEPTABLE_LAYER_TYPES.end()) {
		throw std::invalid_argument("Invalid layer type specified: " + layerType);
	}
	// normal doesn't show up in the xml
	if (layerType.find("normal") != std::string::npos) this->root.remove_attribute("layerType");
	else {
		if (this->root.attribute("layerType").empty()) this->root.append_attribute("layerType");
		this->root.attribute("layerType").set_value(layerType.c_str());
	}
	this->layerType = layerType;
}
bool Layer::isLocked() const noexcept {
	return this->locked;
}
void Layer::setLocked(bool locked) noexcept {
	if (!locked) this->root.remove_attribute("locked");
	else {
		if (this->root.attribute("locked").empty()) this->root.append_attribute("locked");
		this->root.attribute("locked").set_value(locked);
	}
	this->locked = locked;
}
const std::string& Layer::getName() const noexcept {
	return this->name;
}
void Layer::setName(const std::string& name) noexcept {
	if (this->root.attribute("name").empty()) this->root.append_attribute("name");
	this->root.attribute("name").set_value(name.c_str());
	this->name = name;
}
std::optional<unsigned int> Layer::getParentLayerIndex() const noexcept {
	return this->parentLayerIndex;
}
void Layer::setParentLayerIndex(std::optional<unsigned int> parentLayerIndex) noexcept {
	if (!parentLayerIndex.has_value()) this->root.remove_attribute("parentLayerIndex");
	else {
		if (this->root.attribute("parentLayerIndex").empty()) this->root.append_attribute("parentLayerIndex");
		this->root.attribute("parentLayerIndex").set_value(parentLayerIndex.value());
	}
	this->parentLayerIndex = parentLayerIndex;
}

unsigned int Layer::getFrameCount() const noexcept {
	return this->frames[this->frames.size() - 1]->getStartFrame() + this->frames[this->frames.size() - 1]->getDuration();
}
pugi::xml_node& Layer::getRoot() noexcept {
	return this->root;
}
const pugi::xml_node& Layer::getRoot() const noexcept {
	return this->root;
}