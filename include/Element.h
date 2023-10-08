#ifndef ELEMENT_H
#define ELEMENT_H
#define UNDEF -1.0
#include "pugixml.hpp"
class Element {
protected:
	pugi::xml_node root;
	double width, height;
public:
	Element();
	Element(pugi::xml_node& elementNode);
	~Element();
	Element(const Element& element);
	virtual double getWidth() const = 0;
	void setWidth(double width);
	virtual double getHeight() const = 0;
	void setHeight(double height);
	std::string getType();
	pugi::xml_node& getRoot();
};

#endif // ELEMENT_H